using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace GuMaGoChi {
    public sealed class RunnerObstacleArt:IDisposable {
        readonly Bitmap[,] frames=new Bitmap[3,8];
        readonly Dictionary<int,Bitmap> poses=new Dictionary<int,Bitmap>();
        public RunnerObstacleArt(){using(var art=new DefenseArt())for(int kind=0;kind<3;kind++)for(int frame=0;frame<8;frame++){var source=art.Frame("enemies",kind==2?5:kind,frame);if(source==null)throw new InvalidOperationException("달리기 장애물 이미지가 없습니다.");using(var cut=Sprites.Cutout(source)){int size=kind==0?48:kind==1?56:64;var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;float scale=Math.Min((float)size/cut.Width,(float)size/cut.Height);int w=(int)(cut.Width*scale),h=(int)(cut.Height*scale);g.DrawImage(cut,new Rectangle((size-w)/2,size-h,w,h));}image.RotateFlip(RotateFlipType.RotateNoneFlipX);frames[kind,frame]=image;}}}
        public Bitmap Image(RunObstacle obstacle,double elapsed){int kind=(int)obstacle.Kind,frame=(int)(elapsed/.12)%8;if(kind==0)return frames[0,frame];
            int pose=(kind==1?obstacle.JumpPreparation:obstacle.State==RunnerObstacleState.Warning)?1:kind==2&&obstacle.State!=RunnerObstacleState.Active?2:0;
            int rise=kind==2?(int)Math.Round(Math.Max(0,obstacle.Emergence)*7):7,key=kind*10000+pose*1000+rise*10+frame;Bitmap image;if(poses.TryGetValue(key,out image))return image;int size=kind==1?56:64;image=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(image)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                if(kind==1){int height=pose==1?34:56;g.DrawImage(frames[kind,pose==1?0:frame],new Rectangle(0,size-height,size,height));if(pose==1)using(var pen=new Pen(Color.Gold,2))g.DrawLines(pen,new[]{new Point(11,49),new Point(20,36),new Point(30,49)});}
                else{int shake=pose==1?(frame%2==0?-2:2):0;using(var soil=new SolidBrush(Color.FromArgb(128,82,46)))g.FillEllipse(soil,4+shake,48,56,16);using(var light=new Pen(Color.FromArgb(207,154,87),2)){g.DrawArc(light,8+shake,42,48,22,185,170);if(pose==1){g.DrawLine(light,13,38-frame%3*2,9,31);g.DrawLine(light,47,37-frame%2*3,52,29);}}
                    if(pose==0&&rise>0){int height=48*rise/7;var state=g.Save();g.SetClip(new Rectangle(0,size-height,size,height));g.DrawImage(frames[kind,frame],new Rectangle(8,16,48,48));g.Restore(state);}}
            }poses[key]=image;return image;
        }
        public void Dispose(){foreach(var image in frames)if(image!=null)image.Dispose();foreach(var image in poses.Values)image.Dispose();poses.Clear();}
    }
}
