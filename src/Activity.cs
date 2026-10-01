using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GuMaGoChi {
    public class ActivityWindow:Form {
        DesktopApp app;public Pet Pet;bool training,dragging,flying,returning,paused=false,done=false;
        PointF ball,source,velocity,dragPoint;Rectangle basket;int rounds=0,goals=0;double floor;
        Timer timer;Stopwatch watch=Stopwatch.StartNew();double previous=0;
        Button close;string feedback="";double feedbackTime=0,windup=0;
        public ActivityWindow(DesktopApp owner,Pet pet,bool train) {
            app=owner;Pet=pet;training=train;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;Font=new Font("맑은 고딕",10);
            Rectangle area=Screen.FromPoint(new Point(pet.X+90,pet.Y+140)).WorkingArea;Bounds=area;StartPosition=FormStartPosition.Manual;Bounds=area;
            close=new Button {Text="활동 끝내기",Width=120,Height=34,Left=Width-140,Top=18,BackColor=Art.Cream};close.Click+=(s,e)=>CancelActivity();Controls.Add(close);
            floor=Height-40;ResetBall();timer=new Timer {Interval=25};timer.Tick+=Frame;timer.Start();
            MouseDown+=Down;MouseMove+=MoveMouse;MouseUp+=Up;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)CancelActivity();};
            FormClosed+=(s,e)=>{timer.Stop();timer.Dispose();PetWindow w;if(app.Windows.TryGetValue(Pet.Id,out w))w.Animate("stand");if(app.Activity==this)app.Activity=null;};
        }
        void ResetBall() {
            PetWindow actor;if(app.Windows.TryGetValue(Pet.Id,out actor))actor.Animate("stand");
            source=new PointF(Math.Max(70,Math.Min(Width-80,Pet.X-Left+88)),(float)floor-30);ball=source;
            int distance=Pet.SpeciesId<0?210:320;bool right=source.X+distance<Width-85;int x=(int)(source.X+(right?distance:-distance));x=Math.Max(30,Math.Min(Width-110,x));
            basket=new Rectangle(x,(int)floor-85,75,85);flying=false;returning=false;dragging=false;Invalidate();
        }
        public void SetPaused(bool value) {paused=value;previous=watch.Elapsed.TotalSeconds;Capture=false;dragging=false;Invalidate();}
        public void CancelActivity() {if(done)return;done=true;Close();}
        void Down(object s,MouseEventArgs e) {if(paused||app.Paused||flying||returning||done)return;if(e.Button==MouseButtons.Left && Distance(e.Location,ball)<28) {dragging=true;dragPoint=e.Location;Capture=true;Invalidate();}}
        void MoveMouse(object s,MouseEventArgs e) {if(!dragging||paused||app.Paused)return;float dx=e.X-source.X,dy=e.Y-source.Y;double length=Math.Sqrt(dx*dx+dy*dy);if(length>140){dx=(float)(dx*140/length);dy=(float)(dy*140/length);}dragPoint=new PointF(source.X+dx,source.Y+dy);Invalidate();}
        void Up(object s,MouseEventArgs e) {
            if(!dragging)return;dragging=false;Capture=false;if(paused||app.Paused)return;
            float dx=source.X-dragPoint.X,dy=source.Y-dragPoint.Y;if(Math.Abs(dx)+Math.Abs(dy)<8)return;
            double error=training?(Pet.SpeciesId<0?0.08:Pet.Skill=="정밀"?.015:.05):0;
            if(Pet.Skill=="집중")error*=.65;error*=Math.Max(.4,1-Pet.Training*.015);
            double angle=(app.Engine.Random.NextDouble()-.5)*error;float strength=Pet.Skill=="힘"?5.8f:5.3f;
            velocity=new PointF((float)((dx*Math.Cos(angle)-dy*Math.Sin(angle))*strength),(float)((dx*Math.Sin(angle)+dy*Math.Cos(angle))*strength));
            flying=true;feedback=training?Pet.Name+"가 공을 쐈어요!":"공을 쫓아가요!";feedbackTime=2;
            windup=.625;
            PetWindow actor;if(app.Windows.TryGetValue(Pet.Id,out actor))actor.Animate("throw",velocity.X<0);
        }
        static double Distance(PointF a,PointF b) {return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
        void Frame(object s,EventArgs e) {
            double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.06,Math.Max(0,now-previous));previous=now;
            if(app.Paused||paused){Invalidate();return;}
            feedbackTime-=dt;if(feedbackTime<=0)feedback="";
            if(!app.Engine.CanCare(Pet)){CancelActivity();return;}
            if(flying&&windup>0)windup=Math.Max(0,windup-dt);
            else if(flying) {
                PointF old=ball;velocity.Y+=(float)(650*dt);ball.X+=velocity.X*(float)dt;ball.Y+=velocity.Y*(float)dt;
                if(ball.Y<16){ball.Y=16;velocity.Y=Math.Abs(velocity.Y)*.4f;}
                if(ball.X<16){ball.X=16;velocity.X=Math.Abs(velocity.X)*.4f;}if(ball.X>Width-16){ball.X=Width-16;velocity.X=-Math.Abs(velocity.X)*.4f;}
                bool goal=training && velocity.Y>0 && old.Y<=basket.Top+9 && ball.Y>=basket.Top+9 && ball.X>=basket.Left+5 && ball.X<=basket.Right-5;
                if(goal || ball.Y>=floor-12) {
                    if(goal){goals++;ball=new PointF(basket.Left+basket.Width/2,basket.Top+30);feedback="골인! "+Pet.Name+"가 뿌듯해해요.";}
                    else {ball.Y=(float)floor-12;feedback=training?"아깝다! 다음 공도 해볼까?":"공을 주우러 가요.";}
                    feedbackTime=3;flying=false;returning=true;
                }
            }
            if(returning) {
                PetWindow petWindow;if(app.Windows.TryGetValue(Pet.Id,out petWindow)) {
                    int target=Math.Max(Left,Math.Min(Right-petWindow.Width,(int)(ball.X+Left-88)));double speed=Pet.SpeciesId<0?150:Pet.Skill=="민첩"?310:220;
                    int difference=target-petWindow.Left;petWindow.Animate("walk",difference<0);petWindow.Left+=Math.Sign(difference)*(int)Math.Min(Math.Abs(difference),Math.Max(1,speed*dt));Pet.X=petWindow.Left;Pet.Y=petWindow.Top;
                    if(Math.Abs(difference)<8) {rounds++;returning=false;
                        if(rounds>=3) {app.Change(()=>{app.Engine.FinishActivity(Pet,training,goals,rounds);app.Say(Pet,training?"훈련 끝! "+goals+" / 3 골인\n함께 연습해서 좋았어요.":"공을 가져왔어요! 칭찬해 줘요.");});CancelActivity();return;}
                        ResetBall();
                    }
                }else {CancelActivity();return;}
            }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var brush=new SolidBrush(Art.Cream))g.FillRectangle(brush,18,18,Math.Min(Width-180,710),88);
            TextRenderer.DrawText(g,(training?"골인 훈련":"공 가져오기")+" · "+Pet.Name+" · "+rounds+" / 3회",new Font("맑은 고딕",12,FontStyle.Bold),new Point(32,28),Art.Ink);
            TextRenderer.DrawText(g,app.Paused||paused?"일시정지 중이에요.":"공을 뒤로 당겼다 놓으면 고구마가 직접 쏴요.  /  Esc: 끝내기",Font,new Point(32,58),Art.Ink);
            if(feedback!="")TextRenderer.DrawText(g,feedback,Font,new Point(32,82),Art.Green);
            if(training) {using(var b=new SolidBrush(Color.FromArgb(108,139,102)))g.FillRectangle(b,basket);using(var p=new Pen(Art.Ink,4)) {g.DrawRectangle(p,basket);g.DrawLine(p,basket.Left-5,basket.Top,basket.Right+5,basket.Top);}TextRenderer.DrawText(g,"GOAL",Font,new Point(basket.Left+10,basket.Top+28),Color.White);}
            if(dragging) {
                using(var pen=new Pen(Art.Ink,3))g.DrawLine(pen,source,dragPoint);
                float vx=(source.X-dragPoint.X)*(Pet.Skill=="힘"?5.8f:5.3f),vy=(source.Y-dragPoint.Y)*(Pet.Skill=="힘"?5.8f:5.3f);
                for(int i=1;i<=12;i++) {float t=i*.055f,x=source.X+vx*t,y=source.Y+vy*t+325*t*t;if(y>floor)break;g.FillEllipse(Brushes.DarkOliveGreen,x-3,y-3,6,6);}
            }
            g.FillEllipse(Brushes.Orange,ball.X-14,ball.Y-14,28,28);using(var p=new Pen(Art.Ink,2))g.DrawEllipse(p,ball.X-14,ball.Y-14,28,28);
            g.DrawArc(Pens.White,ball.X-9,ball.Y-9,16,16,200,85);
            if(!flying&&!returning&&!dragging)TextRenderer.DrawText(g,"당겨서 쏘기",Font,new Point((int)ball.X-40,(int)ball.Y-48),Art.Ink,Art.Cream);
        }
    }
}
