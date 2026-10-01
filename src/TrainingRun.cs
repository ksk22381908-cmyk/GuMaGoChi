using System;
using System.Drawing;

namespace GuMaGoChi {
    public class TrainingRun {
        public int Stage {get;private set;}
        public int Attempts {get;private set;}
        public int Goals {get;private set;}
        public int TotalGoals {get;private set;}
        public int Limit {get{return Stage==0?3:Stage==1?5:7;}}
        public int Reward {get{return Stage+1;}}
        public bool Complete {get{return Attempts==Limit;}}
        bool inFlight,scored;
        public bool BeginShot(){if(inFlight||Complete)return false;inFlight=true;scored=false;return true;}
        public bool Score(){if(!inFlight||scored)return false;scored=true;Goals++;TotalGoals++;return true;}
        public void EndShot(){if(!inFlight)return;inFlight=false;Attempts++;}
        public bool Advance(){if(!Complete||Goals!=Limit||Stage>=2)return false;Stage++;Attempts=Goals=0;return true;}
    }
    public static class BallPhysics {
        public const float Radius=14,Gravity=650,MaxDrag=280;
        public static float Strength(int width,bool strong){return (float)Math.Sqrt(Math.Max(200,width-32)*Gravity)/MaxDrag*(strong?1.08f:1);}
        public static PointF Pull(PointF source,PointF cursor,int width,int height){
            float dx=source.X-cursor.X,dy=source.Y-cursor.Y;
            float xRoom=dx>=0?source.X-16:width-16-source.X,yRoom=dy>=0?source.Y-16:height-16-source.Y;
            dx*=MaxDrag/Math.Max(16,Math.Min(MaxDrag,xRoom));dy*=MaxDrag/Math.Max(16,Math.Min(MaxDrag,yRoom));
            double length=Math.Sqrt(dx*dx+dy*dy);if(length>MaxDrag){dx=(float)(dx*MaxDrag/length);dy=(float)(dy*MaxDrag/length);}return new PointF(dx,dy);
        }
        public static bool TouchesTop(PointF from,PointF to,Rectangle box){
            // Sweep the ball across the expanded top segment; do not miss fast shots between frames.
            float low=box.Top-Radius-2,high=box.Top+Radius+2;
            double enter=0,leave=1,dy=to.Y-from.Y;
            if(Math.Abs(dy)<.0001){if(from.Y<low||from.Y>high)return false;}
            else {double a=(low-from.Y)/dy,b=(high-from.Y)/dy;enter=Math.Max(0,Math.Min(a,b));leave=Math.Min(1,Math.Max(a,b));if(enter>leave)return false;}
            double x1=from.X+(to.X-from.X)*enter,x2=from.X+(to.X-from.X)*leave;
            return Math.Max(x1,x2)>=box.Left-5-Radius&&Math.Min(x1,x2)<=box.Right+5+Radius;
        }
        public static bool HitsBody(PointF from,PointF to,Rectangle body){
            body.Inflate((int)Radius,(int)Radius);double t0=0,t1=1;
            double[] origins={from.X,from.Y},delta={to.X-from.X,to.Y-from.Y},mins={body.Left,body.Top},maxs={body.Right,body.Bottom};
            for(int i=0;i<2;i++){if(Math.Abs(delta[i])<.0001){if(origins[i]<mins[i]||origins[i]>maxs[i])return false;}else{double a=(mins[i]-origins[i])/delta[i],b=(maxs[i]-origins[i])/delta[i];t0=Math.Max(t0,Math.Min(a,b));t1=Math.Min(t1,Math.Max(a,b));if(t0>t1)return false;}}
            return true;
        }
    }
}
