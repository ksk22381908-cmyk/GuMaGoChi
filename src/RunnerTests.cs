using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GuMaGoChi {
    public static class RunnerTests {
        static int count;
        static void Check(bool ok,string label){if(!ok)throw new Exception("RUNNER FAIL: "+label);count++;}
        public static int Run(){count=0;var r=new EdgeRun(1200,700);PointF p;float angle;
            using(var actorMask=new Bitmap(20,20))using(var bugMask=new Bitmap(20,20)){
                actorMask.SetPixel(5,10,Color.Purple);bugMask.SetPixel(5,10,Color.Green);
                Check(RunnerCollision.Hit(actorMask,new PointF(30,30),0,bugMask,new PointF(30,30),0),"opaque pixels collide");
                Check(!RunnerCollision.Hit(actorMask,new PointF(30,30),0,bugMask,new PointF(31,30),0),"overlapping boxes but separated silhouettes do not collide");
                Check(!RunnerCollision.Hit(actorMask,new PointF(30,30),0,bugMask,new PointF(30,30),90),"rotation affects collision silhouette");
                Check(RunnerCollision.Hit(actorMask,new PointF(30,30),90,bugMask,new PointF(30,30),90),"rotated opaque pixels collide");
                Check(!RunnerCollision.Hit(actorMask,new PointF(30,30),0,bugMask,new PointF(300,300),0),"distant silhouettes do not collide");
            }
            r.Distance=r.Obstacles[0]-5;r.Tick(.02,()=>false);Check(!r.Finished,"live silhouette callback replaces old distance threshold");r.Tick(.02,()=>true);Check(r.Finished,"live silhouette overlap ends run");r=new EdgeRun(1200,700);
            r.Point(300,42,out p,out angle);Check(p.X==300&&p.Y==658&&angle==0,"bottom straight");
            r.Point(1200,42,out p,out angle);Check(p.X<1158&&p.Y<658&&angle==315,"rounded right turn midpoint");
            r.Point(1900,42,out p,out angle);Check(angle==225,"rounded top turn midpoint");
            r.Point(3100,42,out p,out angle);Check(angle==135,"rounded left turn midpoint");
            r.Point(0,42,out p,out angle);PointF wrap;float wrapAngle;r.Point(3800,42,out wrap,out wrapAngle);Check(p==wrap&&angle==wrapAngle,"continuous lap wrap");
            foreach(double boundary in new[]{0.0,1104,1200,1296,1804,1900,1996,3004,3100,3196,3704,3800}){PointF before,after;float a,b;r.Point(boundary-.01,42,out before,out a);r.Point(boundary+.01,42,out after,out b);double change=Math.Abs(a-b);change=Math.Min(change,360-change);Check(Math.Abs(before.X-after.X)<.1&&Math.Abs(before.Y-after.Y)<.1&&change<.1,"continuous position and rotation at corner boundary");r.Point(boundary,119,out after,out b);Check(after.X>=42&&after.X<=1158&&after.Y>=42&&after.Y<=658,"jump stays on screen at corners");}
            foreach(double obstacle in r.Obstacles){double pos=obstacle;double nearest=Math.Min(Math.Min(Math.Abs(pos),Math.Abs(pos-1200)),Math.Min(Math.Abs(pos-1900),Math.Min(Math.Abs(pos-3100),Math.Abs(pos-3800))));Check(nearest>=100,"corner safety zone");}
            double initialObstacle=r.Obstacles[0];r.MoveObstacles(.1);Check(r.Obstacles[0]<initialObstacle,"obstacles move opposite runner");
            r.Obstacles[0]=1200+1;r.MoveObstacles(.1);Check(r.Obstacles[0]<1200&&r.Obstacles[0]>1190,"obstacle continuously crosses corner");
            r.Obstacles[0]=1;r.MoveObstacles(.1);Check(r.Obstacles[0]>r.Length-10,"obstacle continuously crosses lap seam");
            Check(r.InCorner(1200)&&r.InCorner(0)&&r.InCorner(3800)&&!r.InCorner(1090)&&!r.InCorner(1310),"half hitbox only within corner arc");
            using(var solid=new Bitmap(20,20))using(var dot=new Bitmap(20,20)){using(var g=Graphics.FromImage(solid))g.Clear(Color.Green);dot.SetPixel(1,10,Color.Purple);Check(RunnerCollision.Hit(dot,new PointF(30,30),0,solid,new PointF(30,30),0),"straight full hitbox includes edge");Check(!RunnerCollision.Hit(dot,new PointF(30,30),0,solid,new PointF(30,30),0,(float)Math.Sqrt(.5)),"corner reduced hitbox excludes outer edge");dot.SetPixel(10,10,Color.Purple);Check(RunnerCollision.Hit(dot,new PointF(30,30),0,solid,new PointF(30,30),0,(float)Math.Sqrt(.5)),"corner reduced hitbox retains center");}
            r=new EdgeRun(1200,700);
            r.Jump();r.Tick(.1);Check(r.JumpHeight>30,"jump rises");double velocity=r.Velocity;r.Jump();Check(r.Velocity==velocity,"no midair double jump");
            r=new EdgeRun(1200,700);for(int i=0;i<1000&&!r.Finished;i++)r.Tick(.02);Check(r.Finished&&r.Distance<400,"ground collision");double distance=r.Distance;r.Tick(.1);r.Jump();Check(r.Distance==distance&&r.JumpHeight==0,"finished freezes");double frozenObstacle=r.Obstacles[0];r.Tick(.1);Check(r.Obstacles[0]==frozenObstacle,"finished freezes obstacles");
            r=new EdgeRun(1200,700);r.Distance=r.Obstacles[0]-5;r.JumpHeight=75;r.Velocity=0;r.Tick(.02);Check(!r.Finished,"jump clears obstacle");
            r=new EdgeRun(1200,700);r.Distance=1200-2;r.Tick(.02);Check(!r.Finished&&r.Distance>1200,"corner crossing");
            var d=new SaveData();var date=new DateTime(2026,10,5);Check(EdgeRun.Reward(d,9,date)==0&&d.RunnerBest==9,"short run no farm reward");Check(EdgeRun.Reward(d,600,date)==6,"run reward cap");for(int i=0;i<5;i++)EdgeRun.Reward(d,600,date);Check(d.RunnerRewardPaid==20&&d.Seeds==20,"daily cap");Check(EdgeRun.Reward(d,300,date.AddDays(1))==3&&d.RunnerBest==600,"daily reset preserves best");
            var loaded=Storage.Decode(Storage.Encode(d));Check(loaded.RunnerBest==600&&loaded.RunnerRewardPaid==3,"record roundtrip");
            var visualData=new SaveData();visualData.Pets.Add(new Pet {Active=false});using(var app=new DesktopApp(visualData,null,false))using(var window=new RunnerWindow(app,new Pet {Name="달리기 시범",SpeciesId=12,EvolutionId=6}))using(var image=window.RecordImage()){Check(image.Width==720&&image.Height==360,"share image size");image.Save(Path.Combine(Paths.BaseDirectory,"runner-record-preview.png"));window.Show();Application.DoEvents();using(var screen=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(screen,new Rectangle(0,0,screen.Width,screen.Height));screen.Save(Path.Combine(Paths.BaseDirectory,"runner-preview.png"));}app.Exit();}
            return count;
        }
    }
}
