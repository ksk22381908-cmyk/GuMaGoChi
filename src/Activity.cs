using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GuMaGoChi {
    public class ActivityWindow:GameForm {
        DesktopApp app;public Pet Pet;bool training,dragging,flying,returning,paused=false,done=false;
        PointF ball,source,velocity,dragPoint;Rectangle basket;int rounds=0,goals=0;double floor;
        Timer timer;Stopwatch watch=Stopwatch.StartNew();double previous=0;
        TrainingRun run=new TrainingRun();HashSet<string> hitPets=new HashSet<string>();bool finished=false;double celebration=0;
        Button close;string feedback="";double feedbackTime=0,windup=0;
        public ActivityWindow(DesktopApp owner,Pet pet,bool train) {
            app=owner;Pet=pet;training=train;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;Font=new Font("맑은 고딕",10);
            Rectangle area=Screen.FromPoint(new Point(pet.X+90,pet.Y+140)).WorkingArea;Bounds=area;StartPosition=FormStartPosition.Manual;Bounds=area;
            close=new Button {Text="활동 끝내기",Width=120,Height=34,Left=Width-140,Top=18,BackColor=Art.Cream};close.Click+=(s,e)=>CancelActivity();Controls.Add(close);
            PetWindow ownerWindow;if(app.Windows.TryGetValue(Pet.Id,out ownerWindow))Owner=ownerWindow;
            KeyPreview=true;ResetBall();timer=new Timer {Interval=25};timer.Tick+=Frame;timer.Start();
            MouseDown+=Down;MouseMove+=MoveMouse;MouseUp+=Up;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)CancelActivity();};
            FormClosed+=(s,e)=>{timer.Stop();timer.Dispose();PetWindow w;if(app.Windows.TryGetValue(Pet.Id,out w))w.Animate("stand");if(app.Activity==this)app.Activity=null;};
        }
        void ResetBall() {
            PetWindow actor;if(app.Windows.TryGetValue(Pet.Id,out actor))actor.Animate("stand");
            PetWindow actorWindow;app.Windows.TryGetValue(Pet.Id,out actorWindow);Point anchor=actorWindow==null?PetWindow.BallAnchor:actorWindow.ScaledBallAnchor;int floorOffset=actorWindow==null?PetWindow.FloorOffset:actorWindow.ScaledFloorOffset;
            floor=Math.Max(80,Math.Min(Height-12,Pet.Y-Top+floorOffset));
            source=new PointF(Math.Max(42,Math.Min(Width-56,Pet.X-Left+anchor.X)),(float)floor-(floorOffset-anchor.Y));ball=source;
            int distance=training?(int)(Width*(run.Stage==0?.22:run.Stage==1?.48:.88)):(Pet.SpeciesId<0?210:320);
            bool right=source.X<Width/2;
            if(training&&run.Stage==2){PetWindow w;if(app.Windows.TryGetValue(Pet.Id,out w)){w.Location=w.Clamp(new Point(Left+(right?24:Width-w.Width-24),w.Top));Pet.X=w.Left;Pet.Y=w.Top;source.X=w.Left-Left+w.ScaledBallAnchor.X;ball=source;}}
            int x=training&&run.Stage==2?(right?Width-100:24):(int)(source.X+(right?distance:-distance));x=Math.Max(24,Math.Min(Width-100,x));
            int y=training?Math.Max(165,Math.Min(Height-105,(int)source.Y+app.Engine.Random.Next(-160,61))):(int)floor-85;
            basket=new Rectangle(x,y,75,85);if(training)floor=Height-12;flying=false;returning=false;dragging=false;Invalidate();
        }
        public void SetPaused(bool value) {paused=value;previous=watch.Elapsed.TotalSeconds;Capture=false;dragging=false;Invalidate();}
        public void CancelActivity() {if(done)return;done=true;Close();}
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData) {if(keyData==Keys.Escape){CancelActivity();return true;}return base.ProcessCmdKey(ref msg,keyData);}
        void Down(object s,MouseEventArgs e) {if(paused||app.Paused||flying||returning||done||finished||celebration>0)return;if(e.Button==MouseButtons.Left && Distance(e.Location,ball)<28) {dragging=true;dragPoint=e.Location;Capture=true;Invalidate();}}
        void MoveMouse(object s,MouseEventArgs e) {if(!dragging||paused||app.Paused)return;float dx=e.X-source.X,dy=e.Y-source.Y;double length=Math.Sqrt(dx*dx+dy*dy);if(length>BallPhysics.MaxDrag){dx=(float)(dx*BallPhysics.MaxDrag/length);dy=(float)(dy*BallPhysics.MaxDrag/length);}dragPoint=new PointF(source.X+dx,source.Y+dy);Invalidate();}
        void Up(object s,MouseEventArgs e) {
            if(!dragging)return;dragging=false;Capture=false;if(paused||app.Paused)return;
            if(Distance(source,dragPoint)<8)return;PointF pull=BallPhysics.Pull(source,dragPoint,Width,Height);float dx=pull.X,dy=pull.Y;
            double error=training?(Pet.SpeciesId<0?0.08:Pet.Skill=="정밀"?.015:.05):0;
            if(Pet.Skill=="집중")error*=.65;error*=Math.Max(.4,1-Pet.Training*.015);
            double angle=(app.Engine.Random.NextDouble()-.5)*error;float strength=BallPhysics.Strength(Width,Pet.Skill=="힘");
            velocity=new PointF((float)((dx*Math.Cos(angle)-dy*Math.Sin(angle))*strength),(float)((dx*Math.Sin(angle)+dy*Math.Cos(angle))*strength));
            flying=true;feedback=training?Pet.Name+"가 공을 쐈어요!":"공을 쫓아가요!";feedbackTime=2;
            hitPets.Clear();if(training){if(!run.BeginShot()){flying=false;return;}app.Change(()=>app.Engine.RecordShot(Pet));}
            windup=.625;
            PetWindow actor;if(app.Windows.TryGetValue(Pet.Id,out actor))actor.Animate("throw",velocity.X<0);
        }
        static double Distance(PointF a,PointF b) {return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
        public void EnsureForeground(){if(!IsDisposed&&IsHandleCreated&&Visible)Native.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);}
        public static bool CanHit(Pet actor,Pet target){return actor!=target&&actor.Id!=target.Id&&target.Active&&!target.Home&&!target.Sleeping&&!target.GrowthReady&&!target.Dead;}
        void HitOthers(PointF old,PointF current) {
            foreach(PetWindow w in app.Windows.Values){
                Pet target=w.Pet;if(!w.Visible||!CanHit(Pet,target)||hitPets.Contains(target.Id))continue;
                Rectangle sprite=w.ScaledSpriteBounds;Rectangle body=new Rectangle(w.Left-Left+sprite.X,w.Top-Top+sprite.Y,sprite.Width,sprite.Height);
                if(!BallPhysics.HitsBody(old,current,body))continue;
                string reaction=Dialogue.Hit(target);hitPets.Add(target.Id);w.Say(reaction);w.Animate("throw",velocity.X<0);
                ball.X=velocity.X>0?body.Left-BallPhysics.Radius-1:body.Right+BallPhysics.Radius+1;
                velocity.X=-velocity.X*.55f;velocity.Y=-Math.Max(100,Math.Abs(velocity.Y)*.4f);
                feedback=target.Name+": "+reaction;feedbackTime=4;break;
            }
        }
        void Frame(object s,EventArgs e) {
            double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.06,Math.Max(0,now-previous));previous=now;
            if(app.Paused||paused){Invalidate();return;}
            EnsureForeground();if(finished){Invalidate();return;}
            if(celebration>0){celebration-=dt;if(celebration<=0)ResetBall();Invalidate();return;}
            feedbackTime-=dt;if(feedbackTime<=0)feedback="";
            if(!app.Engine.CanCare(Pet)){CancelActivity();return;}
            if(flying&&windup>0)windup=Math.Max(0,windup-dt);
            else if(flying) {
                PointF old=ball;velocity.Y+=BallPhysics.Gravity*(float)dt;ball.X+=velocity.X*(float)dt;ball.Y+=velocity.Y*(float)dt;
                if(ball.Y<16){ball.Y=16;velocity.Y=Math.Abs(velocity.Y)*.4f;}
                if(ball.X<16){ball.X=16;velocity.X=Math.Abs(velocity.X)*.4f;}if(ball.X>Width-16){ball.X=Width-16;velocity.X=-Math.Abs(velocity.X)*.4f;}
                if(!training)HitOthers(old,ball);
                bool goal=training&&BallPhysics.TouchesTop(old,ball,basket);
                if(goal || ball.Y>=floor-12) {
                    if(goal){goals++;if(run.Score())app.Change(()=>app.Engine.ScoreGoal(Pet,run.Reward,run.TotalGoals));ball=new PointF(basket.Left+basket.Width/2,basket.Top+30);feedback="골인! 씨앗 +"+run.Reward+" · "+Pet.Name+"가 뿌듯해해요.";}
                    else {ball.Y=(float)floor-12;feedback=training?"아깝다! 다음 공도 해볼까?":"공을 주우러 가요.";}
                    feedbackTime=3;flying=false;returning=true;
                }
            }
            if(returning) {
                PetWindow petWindow;if(app.Windows.TryGetValue(Pet.Id,out petWindow)) {
                    Point target=petWindow.Clamp(new Point((int)(ball.X+Left-petWindow.ScaledBallAnchor.X),(int)(ball.Y+Top-petWindow.ScaledBallAnchor.Y)));double speed=Pet.SpeciesId<0?150:Pet.Skill=="민첩"?310:220;
                    double dx=target.X-petWindow.Left,dy=target.Y-petWindow.Top,distance=Math.Sqrt(dx*dx+dy*dy);petWindow.Animate("walk",dx<0);
                    if(distance>0){double step=Math.Min(distance,Math.Max(1,speed*dt));petWindow.Location=new Point(petWindow.Left+(int)Math.Round(dx/distance*step),petWindow.Top+(int)Math.Round(dy/distance*step));}Pet.X=petWindow.Left;Pet.Y=petWindow.Top;
                    if(distance<8) {rounds++;returning=false;
                        if(training){run.EndShot();if(run.Complete){if(run.Stage==0)app.Change(()=>app.Engine.FinishActivity(Pet,true));if(run.Advance()){rounds=goals=0;celebration=3;feedback="축하해요! 전부 골인! "+run.Limit+"회 도전 · 골인당 씨앗 +"+run.Reward;feedbackTime=4;petWindow.Animate("stand");}else{finished=true;close.Text="기록 닫기";feedback="훈련 끝! 최종 "+run.TotalGoals+" / 15점 · 최고 "+app.Engine.Data.BestGoals+"점";feedbackTime=Double.MaxValue;petWindow.Animate("stand");app.Say(Pet,Dialogue.Activity(Pet,true));}return;}}
                        if(!training&&rounds>=3) {app.Change(()=>{app.Engine.FinishActivity(Pet,training);app.Say(Pet,Dialogue.Activity(Pet,training));});CancelActivity();return;}
                        ResetBall();
                    }
                }else {CancelActivity();return;}
            }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var brush=new SolidBrush(Art.Cream))g.FillRectangle(brush,18,18,Math.Min(Width-180,820),120);
            TextRenderer.DrawText(g,(training?"골인 훈련":"공 가져오기")+" · "+Pet.Name+" · "+rounds+" / "+(training?run.Limit:3)+"회",new Font("맑은 고딕",12,FontStyle.Bold),new Point(32,28),Art.Ink);
            TextRenderer.DrawText(g,app.Paused||paused?"일시정지 중이에요.":"공을 뒤로 당겼다 놓으면 고구마가 직접 쏴요.  /  Esc: 끝내기",Font,new Point(32,58),Art.Ink);
            if(training)TextRenderer.DrawText(g,"현재 "+run.TotalGoals+" / 15점   최고 기록 "+app.Engine.Data.BestGoals+" / 15점   · "+(run.Stage==0?"가까운 거리":run.Stage==1?"먼 거리":"최대 거리"),Font,new Point(32,82),Art.Ink);
            if(feedback!="")TextRenderer.DrawText(g,feedback,Font,new Point(32,training?110:82),Art.Green);
            // Keep only the score panel after completion; gameplay objects must disappear.
            if(finished)return;
            if(training) {using(var b=new SolidBrush(Color.FromArgb(108,139,102)))g.FillRectangle(b,basket);using(var p=new Pen(Art.Ink,4)) {g.DrawRectangle(p,basket);g.DrawLine(p,basket.Left-5,basket.Top,basket.Right+5,basket.Top);}TextRenderer.DrawText(g,"GOAL",Font,new Point(basket.Left+10,basket.Top+28),Color.White);}
            if(dragging) {
                using(var pen=new Pen(Art.Ink,3))g.DrawLine(pen,source,dragPoint);
                PointF pull=BallPhysics.Pull(source,dragPoint,Width,Height);float vx=pull.X*BallPhysics.Strength(Width,Pet.Skill=="힘"),vy=pull.Y*BallPhysics.Strength(Width,Pet.Skill=="힘");
                for(int i=1;i<=6;i++) {float t=i*.08f,x=source.X+vx*t,y=source.Y+vy*t+325*t*t;if(y>floor)break;g.FillEllipse(Brushes.DarkOliveGreen,x-3,y-3,6,6);}
            }
            g.FillEllipse(Brushes.Orange,ball.X-14,ball.Y-14,28,28);using(var p=new Pen(Art.Ink,2))g.DrawEllipse(p,ball.X-14,ball.Y-14,28,28);
            g.DrawArc(Pens.White,ball.X-9,ball.Y-9,16,16,200,85);
            if(!flying&&!returning&&!dragging)TextRenderer.DrawText(g,"당겨서 쏘기",Font,new Point((int)ball.X-40,(int)ball.Y-48),Art.Ink,Art.Cream);
        }
    }
}
