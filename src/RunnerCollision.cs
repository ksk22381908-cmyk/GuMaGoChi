using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GuMaGoChi {
    public sealed class RunnerCollision:IDisposable {
        Bitmap actorBuffer,bugBuffer;
        int[] actorRow,bugRow;
        public void Dispose(){if(actorBuffer!=null)actorBuffer.Dispose();if(bugBuffer!=null)bugBuffer.Dispose();actorBuffer=bugBuffer=null;}
        public static void Draw(Graphics g,Bitmap image,PointF center,float angle,float scale=1){var state=g.Save();g.TranslateTransform(center.X,center.Y);g.RotateTransform(angle);g.ScaleTransform(scale,scale);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;g.DrawImage(image,new Rectangle(-image.Width/2,-image.Height/2,image.Width,image.Height));g.Restore(state);}
        public static bool Hit(Bitmap actor,PointF actorCenter,float actorAngle,Bitmap bug,PointF bugCenter,float bugAngle,float bugScale=1){
            using(var collision=new RunnerCollision())return collision.Test(actor,actorCenter,actorAngle,bug,bugCenter,bugAngle,bugScale);
        }
        public bool Test(Bitmap actor,PointF actorCenter,float actorAngle,Bitmap bug,PointF bugCenter,float bugAngle,float bugScale=1){
            double radius=(Math.Sqrt(actor.Width*actor.Width+actor.Height*actor.Height)+Math.Sqrt(bug.Width*bug.Width+bug.Height*bug.Height))/2;
            if(Math.Abs(actorCenter.X-bugCenter.X)>radius||Math.Abs(actorCenter.Y-bugCenter.Y)>radius)return false;
            int size=(int)Math.Ceiling(radius*2)+4;float x=(float)Math.Floor(Math.Min(actorCenter.X,bugCenter.X)-radius),y=(float)Math.Floor(Math.Min(actorCenter.Y,bugCenter.Y)-radius);
            if(actorBuffer==null||actorBuffer.Width<size){Dispose();actorBuffer=new Bitmap(size,size,PixelFormat.Format32bppArgb);bugBuffer=new Bitmap(size,size,PixelFormat.Format32bppArgb);actorRow=new int[size];bugRow=new int[size];}
            var a=actorBuffer;var b=bugBuffer;
                using(var g=Graphics.FromImage(a)){g.Clear(Color.Transparent);Draw(g,actor,new PointF(actorCenter.X-x,actorCenter.Y-y),actorAngle);}
                using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);Draw(g,bug,new PointF(bugCenter.X-x,bugCenter.Y-y),bugAngle,bugScale);}
                var rect=new Rectangle(0,0,size,size);var ad=a.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var bd=b.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                bool hit=false;var ar=actorRow;var br=bugRow;for(int row=0;row<size&&!hit;row++){Marshal.Copy(IntPtr.Add(ad.Scan0,row*ad.Stride),ar,0,size);Marshal.Copy(IntPtr.Add(bd.Scan0,row*bd.Stride),br,0,size);for(int col=0;col<size;col++)if(((uint)ar[col]>>24)>=230&&((uint)br[col]>>24)>=230){hit=true;break;}}
                a.UnlockBits(ad);b.UnlockBits(bd);return hit;
        }
    }
}
