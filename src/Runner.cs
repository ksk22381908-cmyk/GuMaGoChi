using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class EdgeRun {
        public readonly double Width,Height,Length;
        public readonly List<double> Obstacles=new List<double>();

        public double Distance,JumpHeight,Velocity,Elapsed;
        public bool Finished;
        public int Score {get{return (int)(Distance/20);}}
        public double Speed {get{return Math.Min(520,190+Distance/110);}}
        public EdgeRun(double width,double height){Width=width;Height=height;Length=2*(width+height);double start=0;foreach(double edge in new[]{width,height,width,height}){int count=Math.Max(1,(int)(edge/500));for(int i=0;i<count;i++){Obstacles.Add(start+edge*(i+1)/(count+1));}start+=edge;}}
        public double ObstacleSpeed {get{return Math.Min(100,55+Distance/400);}}
        public void MoveObstacles(double seconds){for(int i=0;i<Obstacles.Count;i++)Obstacles[i]=((Obstacles[i]-ObstacleSpeed*seconds)%Length+Length)%Length;}
        public bool InCorner(double distance){double p=((distance%Length)+Length)%Length,radius=Math.Min(96,Math.Min(Width,Height)/4);foreach(double corner in new[]{0.0,Width,Width+Height,2*Width+Height,Length})if(Math.Abs(p-corner)<=radius)return true;return false;}
        public void Jump(){if(!Finished&&JumpHeight<=0){Velocity=440;}}
        public void Tick(double seconds,Func<bool> collision=null){if(Finished)return;double remain=Math.Max(0,Math.Min(.1,seconds));while(remain>0&&!Finished){double dt=Math.Min(.005,remain);remain-=dt;Elapsed+=dt;MoveObstacles(dt);Distance+=Speed*dt;if(JumpHeight>0||Velocity>0){JumpHeight+=Velocity*dt;Velocity-=1250*dt;if(JumpHeight<=0){JumpHeight=0;Velocity=0;}}if(collision!=null){if(collision())Finished=true;continue;}double pos=Distance%Length;foreach(double obstacle in Obstacles){double gap=Math.Abs(pos-obstacle);gap=Math.Min(gap,Length-gap);if(gap<27&&JumpHeight<38){Finished=true;break;}}}}
        public void Point(double distance,double inward,out PointF point,out float angle) {
            double p=((distance%Length)+Length)%Length;
            double radius=Math.Min(96,Math.Min(Width,Height)/4);
            double[] corners={Width,Width+Height,2*Width+Height,Length};
            for(int corner=0;corner<4;corner++) {
                double delta=p-corners[corner];if(corner==3&&p<radius)delta=p;
                if(Math.Abs(delta)>radius)continue;
                double t=(delta+radius)/(2*radius),theta=Math.PI/2-corner*Math.PI/2-t*Math.PI/2;
                double cx=corner<2?Width-radius:radius,cy=corner==0||corner==3?Height-radius:radius;
                point=new PointF((float)(cx+(radius-inward)*Math.Cos(theta)),(float)(cy+(radius-inward)*Math.Sin(theta)));
                angle=(float)((360-corner*90-t*90)%360);return;
            }
            if(p<Width){point=new PointF((float)p,(float)(Height-inward));angle=0;}
            else if((p-=Width)<Height){point=new PointF((float)(Width-inward),(float)(Height-p));angle=270;}
            else if((p-=Height)<Width){point=new PointF((float)(Width-p),(float)inward);angle=180;}
            else{p-=Width;point=new PointF((float)inward,(float)p);angle=90;}
        }
        public static int Reward(SaveData data,int score,DateTime now){data.RunnerBest=Math.Max(data.RunnerBest,score);string day=now.ToString("yyyy-MM-dd");if(data.RunnerRewardDay!=day){data.RunnerRewardDay=day;data.RunnerRewardPaid=0;}int amount=Math.Min(Math.Max(0,20-data.RunnerRewardPaid),Math.Min(6,Math.Max(0,score/100)));data.RunnerRewardPaid+=amount;data.Seeds+=amount;return amount;}
    }
    public sealed class RunnerWindow:Form {
        readonly DesktopApp app;readonly Pet pet;readonly Timer timer;
        readonly Bitmap[] bugs=new Bitmap[8];readonly Dictionary<int,Bitmap> actors=new Dictionary<int,Bitmap>();readonly Stopwatch clock=Stopwatch.StartNew();
        EdgeRun run;double previous;bool held,settled;int reward;
        readonly Button retry,export;readonly Label info;
        public RunnerWindow(DesktopApp owner,Pet actor){app=owner;pet=actor;Text="고구마 밭 달리기";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;KeyPreview=true;StartPosition=FormStartPosition.Manual;Bounds=Screen.FromPoint(new Point(actor.X,actor.Y)).Bounds;
            var panel=new FlowLayoutPanel {BackColor=Art.Cream,AutoSize=true,Padding=new Padding(10),WrapContents=false};info=new Label {Width=330,Height=55,ForeColor=Color.SaddleBrown};retry=new Button {Text="다시 달리기",Width=110,Height=38,Visible=false};export=new Button {Text="기록 이미지 저장",Width=135,Height=38,Visible=false};var close=new Button {Text="끝내기 · Esc",Width=110,Height=38};panel.Controls.Add(info);panel.Controls.Add(retry);panel.Controls.Add(export);panel.Controls.Add(close);Controls.Add(panel);panel.Location=new Point(Math.Max(110,(Width-740)/2),Math.Max(110,Height/2-40));
            retry.Click+=(s,e)=>Reset();export.Click+=(s,e)=>Export();close.Click+=(s,e)=>Close();MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)Input();};KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)Close();if(e.KeyCode==Keys.Space){if(!held)Input();held=true;e.Handled=true;e.SuppressKeyPress=true;}};KeyUp+=(s,e)=>{if(e.KeyCode==Keys.Space)held=false;};Deactivate+=(s,e)=>held=false;
            using(var art=new DefenseArt())for(int i=0;i<8;i++){var source=art.Frame("enemies",0,i);using(var cut=Sprites.Cutout(source)){bugs[i]=new Bitmap(48,48,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bugs[i])){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;float scale=Math.Min(48f/cut.Width,48f/cut.Height);int w=(int)(cut.Width*scale),h=(int)(cut.Height*scale);g.DrawImage(cut,new Rectangle((48-w)/2,48-h,w,h));}bugs[i].RotateFlip(RotateFlipType.RotateNoneFlipX);}}timer=new Timer {Interval=16};timer.Tick+=(s,e)=>Frame();FormClosed+=(s,e)=>{timer.Stop();timer.Dispose();foreach(var b in bugs)b.Dispose();foreach(var b in actors.Values)b.Dispose();if(app.Runner==this)app.Runner=null;};Reset();timer.Start();}
        void Reset(){run=new EdgeRun(Math.Max(200,ClientSize.Width),Math.Max(200,ClientSize.Height));settled=false;reward=0;held=false;retry.Visible=export.Visible=false;previous=clock.Elapsed.TotalSeconds;UpdateInfo();Invalidate();}
        void Input(){if(!app.Paused&&ContainsFocus&&!run.Finished)run.Jump();}
        void Frame(){double now=clock.Elapsed.TotalSeconds,dt=now-previous;previous=now;if(!app.Engine.CanCare(pet)){Close();return;}if(!app.Paused&&ContainsFocus)run.Tick(dt,Collides);if(run.Finished&&!settled){settled=true;app.Change(()=>reward=EdgeRun.Reward(app.Engine.Data,run.Score,DateTime.Now));retry.Visible=export.Visible=true;}UpdateInfo();Invalidate();}
        Bitmap ActorImage(){double phase=run.Distance/190;int key=(int)(phase*8)%32;Bitmap image;if(!actors.TryGetValue(key,out image)){image=new Bitmap(84,84,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image))Art.Pet(g,pet,new Rectangle(0,0,84,84),phase,"walk");actors[key]=image;}return image;}
        void ActorPosition(out PointF actor,out float rotation){run.Point(run.Distance,42+run.JumpHeight,out actor,out rotation);double radians=rotation*Math.PI/180;float extent=(float)(42*(Math.Abs(Math.Cos(radians))+Math.Abs(Math.Sin(radians))));actor.X=Math.Max(extent,Math.Min((float)run.Width-extent,actor.X));actor.Y=Math.Max(extent,Math.Min((float)run.Height-extent,actor.Y));}
        bool Collides(){PointF actor;float rotation;ActorPosition(out actor,out rotation);Bitmap bug=bugs[(int)(run.Elapsed/.12)%8];foreach(double obstacle in run.Obstacles){PointF point;float angle;run.Point(obstacle,24,out point,out angle);if(RunnerCollision.Hit(ActorImage(),actor,rotation,bug,point,angle,run.InCorner(obstacle)?(float)Math.Sqrt(.5):1))return true;}return false;}
        void UpdateInfo(){info.Text=pet.Name+" · 밭 달리기  "+run.Score+"점 / 최고 "+app.Engine.Data.RunnerBest+"점\n"+(run.Finished?"달리기 끝! 씨앗 +"+reward:app.Paused||!ContainsFocus?"일시정지 · 이 창을 선택하면 계속":"스페이스 / 고구마 클릭: 점프 · 모니터 네 면 달리기");}
        public Bitmap RecordImage(){var image=new Bitmap(720,360);using(var g=Graphics.FromImage(image))using(var title=new Font("맑은 고딕",22,FontStyle.Bold))using(var body=new Font("맑은 고딕",15)){g.Clear(Art.Cream);g.DrawString("고구마 밭 달리기",title,Brushes.SaddleBrown,30,25);Art.Pet(g,pet,new Rectangle(35,105,145,145),0);g.DrawString(pet.Name+" · "+pet.Kind+"\n이번 기록  "+run.Score+"점\n개인 최고  "+app.Engine.Data.RunnerBest+"점",body,Brushes.SaddleBrown,220,115);g.DrawString("GuMaGoChi  ·  "+DateTime.Now.ToString("yyyy-MM-dd"),body,Brushes.SaddleBrown,30,300);}return image;}
        void Export(){using(var d=new SaveFileDialog {Filter="PNG 이미지|*.png",FileName="GuMaGoChi-달리기-"+run.Score+"점.png"})if(d.ShowDialog(this)==DialogResult.OK)try{using(var image=RecordImage())image.Save(d.FileName,ImageFormat.Png);}catch(Exception ex){MessageBox.Show(this,"기록 저장 실패: "+ex.Message);}}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;foreach(double obstacle in run.Obstacles){PointF point;float angle;run.Point(obstacle,24,out point,out angle);RunnerCollision.Draw(g,bugs[(int)(run.Elapsed/.12)%8],point,angle);}PointF actor;float rotation;ActorPosition(out actor,out rotation);RunnerCollision.Draw(g,ActorImage(),actor,rotation);}
    }
}
