using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class EdgeRun {
        public readonly double Width,Height,Length;
        public readonly List<RunObstacle> Obstacles=new List<RunObstacle>();
        readonly Random random;
        readonly double straightWidth,straightHeight,arc;
        public readonly double CornerRadius;
        double jumpBuffer;

        public double Distance,JumpHeight,Velocity,Elapsed;
        public bool Finished;
        public int Score {get{return (int)(Distance/20);}}
        public double Speed {get{return Math.Min(520,190+Distance/110);}}
        public EdgeRun(double width,double height,int seed=0){Width=width;Height=height;random=seed==0?new Random():new Random(seed);CornerRadius=Math.Min(180,Math.Min(width,height)/2-12);straightWidth=width-2*CornerRadius;straightHeight=height-2*CornerRadius;arc=Math.PI*(CornerRadius-42)/2;Length=2*(straightWidth+straightHeight)+4*arc;int count=Math.Max(2,Math.Min(12,(int)(Length/650)));for(int i=0;i<count;i++)Obstacles.Add(new RunObstacle {Distance=(i+.65)*Length/count,Kind=RunnerObstacleKind.Crawler});}
        public double ObstacleSpeed {get{return Math.Min(100,55+Distance/400);}}
        public double Ahead(double distance){return ((distance-Distance)%Length+Length)%Length;}
        public double WarningTime {get{return Math.Max(.85,1.15-Score/4000.0);}}
        public RunnerObstacleKind ChooseKind(int score,double roll){return score>=300&&roll<.10?RunnerObstacleKind.Mole:score>=120&&roll<.40?RunnerObstacleKind.Grasshopper:RunnerObstacleKind.Crawler;}
        public void MoveObstacles(double seconds){foreach(var obstacle in Obstacles){if(obstacle.Kind!=RunnerObstacleKind.Mole)obstacle.Distance=(obstacle.Distance-ObstacleSpeed*seconds+Length)%Length;double ahead=Ahead(obstacle.Distance),behind=Length-ahead;if(ahead<90)obstacle.Encountered=true;if(obstacle.Encountered&&behind>180&&behind<Length/2){obstacle.Pending=true;obstacle.State=RunnerObstacleState.Hidden;}
                if(obstacle.Pending){double spacing=Math.Max(360,(Speed+ObstacleSpeed)*1.35);for(int attempt=0;attempt<4;attempt++){double candidate=(Distance+Math.Max(spacing,Length*.35)+random.NextDouble()*Length*.35)%Length;if(Ahead(candidate)<spacing)continue;bool clear=true;foreach(var other in Obstacles){if(other==obstacle||other.Pending)continue;double gap=Math.Abs(candidate-other.Distance);if(Math.Min(gap,Length-gap)<spacing){clear=false;break;}}if(!clear)continue;obstacle.Distance=candidate;obstacle.Kind=ChooseKind(Score,random.NextDouble());obstacle.WillJump=random.Next(2)==0;obstacle.DecisionAt=0;obstacle.State=RunnerObstacleState.Cruise;obstacle.Clock=0;obstacle.Triggered=obstacle.Encountered=false;obstacle.Pending=false;break;}if(obstacle.Pending)continue;}
                obstacle.Update(seconds,Ahead(obstacle.Distance),Speed+(obstacle.Kind==RunnerObstacleKind.Mole?0:ObstacleSpeed),WarningTime);
            }}
        public bool InCorner(double distance){PointF point;float angle;Point(distance,42,out point,out angle);return Math.Abs(angle%90)>.001;}
        public void Jump(){if(Finished)return;if(JumpHeight<=0&&Velocity<=0){Velocity=440;jumpBuffer=0;}else jumpBuffer=.12;}
        public void Tick(double seconds,Func<bool> collision=null){if(Finished)return;double remain=Math.Max(0,Math.Min(.5,seconds));while(remain>0&&!Finished){double dt=Math.Min(.01,remain);remain-=dt;Elapsed+=dt;Distance+=Speed*dt;MoveObstacles(dt);if(JumpHeight>0||Velocity>0){JumpHeight+=Velocity*dt;Velocity-=1250*dt;if(JumpHeight<=0){JumpHeight=0;Velocity=0;if(jumpBuffer>0)Jump();}}jumpBuffer=Math.Max(0,jumpBuffer-dt);if(collision!=null){if(collision())Finished=true;continue;}foreach(var obstacle in Obstacles){if(!obstacle.Collidable)continue;double gap=Ahead(obstacle.Distance);gap=Math.Min(gap,Length-gap);if(gap<27&&Math.Abs(JumpHeight-obstacle.Height)<38){Finished=true;break;}}}}
        public void Point(double distance,double inward,out PointF point,out float angle) {
            double p=((distance%Length)+Length)%Length;
            double[] straights={straightWidth,straightHeight,straightWidth,straightHeight};
            for(int edge=0;edge<4;edge++){
                if(p<straights[edge]){angle=(360-edge*90)%360;switch(edge){case 0:point=new PointF((float)(CornerRadius+p),(float)(Height-inward));return;case 1:point=new PointF((float)(Width-inward),(float)(Height-CornerRadius-p));return;case 2:point=new PointF((float)(Width-CornerRadius-p),(float)inward);return;default:point=new PointF((float)inward,(float)(CornerRadius+p));return;}}
                p-=straights[edge];if(p<=arc){double turn=p/arc*Math.PI/2,theta=Math.PI/2-edge*Math.PI/2-turn;double cx=edge<2?Width-CornerRadius:CornerRadius,cy=edge==0||edge==3?Height-CornerRadius:CornerRadius;double radius=Math.Max(12,CornerRadius-inward);point=new PointF((float)(cx+radius*Math.Cos(theta)),(float)(cy+radius*Math.Sin(theta)));angle=(float)((360-edge*90-turn*180/Math.PI)%360);return;}p-=arc;
            }
            point=new PointF((float)CornerRadius,(float)(Height-inward));angle=0;
        }
        public static int Reward(SaveData data,int score,DateTime now){data.RunnerBest=Math.Max(data.RunnerBest,score);string day=now.ToString("yyyy-MM-dd");if(data.RunnerRewardDay!=day){data.RunnerRewardDay=day;data.RunnerRewardPaid=0;}int amount=Math.Min(Math.Max(0,20-data.RunnerRewardPaid),Math.Min(6,Math.Max(0,score/100)));data.RunnerRewardPaid+=amount;data.Seeds+=amount;return amount;}
    }
    public sealed class RunnerWindow:Form {
        readonly DesktopApp app;readonly Pet pet;readonly Timer timer;
        readonly RunnerObstacleArt obstacleArt=new RunnerObstacleArt();readonly Dictionary<int,Bitmap> actors=new Dictionary<int,Bitmap>();readonly Stopwatch clock=Stopwatch.StartNew();
        readonly RunnerCollision collision=new RunnerCollision();
        EdgeRun run;double previous,countdown;bool held,settled,wasRunning;int reward;
        readonly Button retry,export;readonly Label info;
        public RunnerWindow(DesktopApp owner,Pet actor){app=owner;pet=actor;Text="고구마 밭 달리기";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;KeyPreview=true;StartPosition=FormStartPosition.Manual;Bounds=Screen.FromPoint(new Point(actor.X,actor.Y)).Bounds;
            var panel=new FlowLayoutPanel {BackColor=Art.Cream,AutoSize=true,Padding=new Padding(10),WrapContents=true,MaximumSize=new Size(Math.Max(200,Width-160),0)};info=new Label {Width=Math.Min(400,Math.Max(160,Width-190)),Height=90,ForeColor=Color.SaddleBrown};retry=UiLayout.DialogButton("다시 달리기");retry.Visible=false;export=UiLayout.DialogButton("기록 이미지 저장");export.Visible=false;var close=UiLayout.DialogButton("끝내기 · Esc");panel.Controls.Add(info);panel.Controls.Add(retry);panel.Controls.Add(export);panel.Controls.Add(close);Controls.Add(panel);panel.Location=new Point(Math.Max(80,(Width-780)/2),Math.Max(80,Height/2-55));
            retry.Click+=(s,e)=>Reset();export.Click+=(s,e)=>Export();close.Click+=(s,e)=>Close();MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left)Input();};KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)Close();if(e.KeyCode==Keys.Space){if(!held)Input();held=true;e.Handled=true;e.SuppressKeyPress=true;}};KeyUp+=(s,e)=>{if(e.KeyCode==Keys.Space)held=false;};Deactivate+=(s,e)=>held=false;
            timer=new Timer {Interval=16};timer.Tick+=(s,e)=>Frame();FormClosed+=(s,e)=>{timer.Stop();timer.Dispose();obstacleArt.Dispose();collision.Dispose();foreach(var b in actors.Values)b.Dispose();if(app.Runner==this)app.Runner=null;};Reset();timer.Start();}
        void Reset(){run=new EdgeRun(Math.Max(200,ClientSize.Width),Math.Max(200,ClientSize.Height));settled=false;reward=0;held=false;wasRunning=false;countdown=2;retry.Visible=export.Visible=false;previous=clock.Elapsed.TotalSeconds;UpdateInfo();Invalidate();}
        void Input(){if(!app.Paused&&ContainsFocus&&!run.Finished&&countdown<=0)run.Jump();}
        void Frame(){double now=clock.Elapsed.TotalSeconds,dt=now-previous;previous=now;if(!app.Engine.CanCare(pet)){Close();return;}bool running=!app.Paused&&ContainsFocus;if(running&&!wasRunning&&run.Elapsed>0&&!run.Finished)countdown=1;wasRunning=running;if(running){if(countdown>0)countdown=Math.Max(0,countdown-Math.Min(.1,dt));else run.Tick(dt,Collides);}if(run.Finished&&!settled){settled=true;app.Change(()=>reward=EdgeRun.Reward(app.Engine.Data,run.Score,DateTime.Now));retry.Visible=export.Visible=true;}UpdateInfo();Invalidate();}
        Bitmap ActorImage(){double phase=run.Distance/190;int key=(int)(phase*8)%32;Bitmap image;if(!actors.TryGetValue(key,out image)){image=new Bitmap(84,84,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image))Art.Pet(g,pet,new Rectangle(0,0,84,84),phase,"walk");actors[key]=image;}return image;}
        void ActorPosition(out PointF actor,out float rotation){run.Point(run.Distance,42+run.JumpHeight,out actor,out rotation);}
        bool Collides(){PointF actor;float rotation;ActorPosition(out actor,out rotation);foreach(var obstacle in run.Obstacles){if(!obstacle.Collidable)continue;PointF point;float angle;var image=obstacleArt.Image(obstacle,run.Elapsed);run.Point(obstacle.Distance,image.Height/2.0+obstacle.Height,out point,out angle);if(collision.Test(ActorImage(),actor,rotation,image,point,angle))return true;}return false;}
        void UpdateInfo(){string tier=run.Score<120?"초급 · 버러지":run.Score<300?"중급 · 메뚜기 추가":"고급 · 두더지 추가";int remaining=app.Engine.Data.RunnerRewardDay==DateTime.Now.ToString("yyyy-MM-dd")?Math.Max(0,20-app.Engine.Data.RunnerRewardPaid):20;info.Text=pet.Name+" · 밭 달리기  "+run.Score+"점 / 최고 "+app.Engine.Data.RunnerBest+"점\n"+(run.Finished?"달리기 끝! 씨앗 +"+reward:app.Paused||!ContainsFocus?"일시정지 · 이 창을 선택하면 계속":countdown>0?"준비 · "+Math.Ceiling(countdown):tier+" · 스페이스 / 고구마 클릭: 점프")+"\n"+(run.Finished?"오늘 남은 보상 "+remaining+"개":run.Score<600?"다음 씨앗까지 "+(100-run.Score%100)+"점 · 오늘 남은 "+remaining+"개":"한 판 보상 최대 6개 · 오늘 남은 "+remaining+"개");}
        public Bitmap RecordImage(){var image=new Bitmap(720,360);using(var g=Graphics.FromImage(image))using(var title=new Font("맑은 고딕",22,FontStyle.Bold))using(var body=new Font("맑은 고딕",15)){g.Clear(Art.Cream);g.DrawString("고구마 밭 달리기",title,Brushes.SaddleBrown,30,25);Art.Pet(g,pet,new Rectangle(35,105,145,145),0);g.DrawString(pet.Name+" · "+pet.Kind+"\n이번 기록  "+run.Score+"점\n개인 최고  "+app.Engine.Data.RunnerBest+"점",body,Brushes.SaddleBrown,220,115);g.DrawString("GuMaGoChi  ·  "+DateTime.Now.ToString("yyyy-MM-dd"),body,Brushes.SaddleBrown,30,300);}return image;}
        void Export(){using(var d=new SaveFileDialog {Filter="PNG 이미지|*.png",FileName="GuMaGoChi-달리기-"+run.Score+"점.png"})if(d.ShowDialog(this)==DialogResult.OK)try{using(var image=RecordImage())image.Save(d.FileName,ImageFormat.Png);}catch(Exception ex){MessageBox.Show(this,"기록 저장 실패: "+ex.Message);}}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;foreach(var obstacle in run.Obstacles){if(!obstacle.Visible)continue;PointF point;float angle;var image=obstacleArt.Image(obstacle,run.Elapsed);run.Point(obstacle.Distance,image.Height/2.0+obstacle.Height,out point,out angle);RunnerCollision.Draw(g,image,point,angle);if(!String.IsNullOrEmpty(obstacle.Speech)){var state=g.Save();g.TranslateTransform(point.X,point.Y);g.RotateTransform(angle);using(var font=new Font("맑은 고딕",10,FontStyle.Bold))using(var format=new StringFormat {Alignment=StringAlignment.Center}){g.FillRectangle(Brushes.PaleGoldenrod,-62,-68,124,28);g.FillPolygon(Brushes.PaleGoldenrod,new[]{new Point(-6,-40),new Point(6,-40),new Point(0,-33)});g.DrawString(obstacle.Speech,font,Brushes.DarkRed,new RectangleF(-62,-66,124,26),format);}g.Restore(state);}}PointF actor;float rotation;ActorPosition(out actor,out rotation);RunnerCollision.Draw(g,ActorImage(),actor,rotation);}
    }
}
