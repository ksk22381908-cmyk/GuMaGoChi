using System;
using System.Drawing;

namespace GuMaGoChi {
    public static class BerryCombatArt {
        public static void Blueberry(Graphics g,PointF point,float size){
            string[] pixels={"    gg    ","   gggg   ","  ######  "," ##hhbbb# "," #hhbbbb# ","#hbbbbddd#","#bbbbbddd#"," #bbbddd# "," ##dddd#  ","   ####   "};
            float unit=size/10;
            using(var outline=new SolidBrush(Color.FromArgb(35,22,61)))using(var blue=new SolidBrush(Color.FromArgb(57,60,166)))using(var dark=new SolidBrush(Color.FromArgb(39,40,118)))using(var light=new SolidBrush(Color.FromArgb(159,170,255)))using(var green=new SolidBrush(Color.FromArgb(94,153,46))){
                for(int y=0;y<pixels.Length;y++)for(int x=0;x<pixels[y].Length;x++){
                    char pixel=pixels[y][x];if(pixel==' ')continue;
                    Brush brush=pixel=='#'?outline:pixel=='b'?blue:pixel=='d'?dark:pixel=='h'?light:green;
                    g.FillRectangle(brush,point.X-size/2+x*unit,point.Y-size/2+y*unit,unit,unit);
                }
            }
        }
        public static void Poison(Graphics g,double age){
            using(var purple=new SolidBrush(Color.FromArgb(122,52,203)))using(var light=new SolidBrush(Color.FromArgb(198,143,247))){
                for(int i=0;i<3;i++){float x=-19+i*17,y=-15-(float)((age*12+i*7)%23);g.FillRectangle(purple,x,y,7,7);g.FillRectangle(light,x+1,y+1,3,3);}
            }
        }
        public static void Beam(Graphics g,PointF start,PointF end,double age,float zoom){
            using(var outer=new Pen(Color.FromArgb(212,30,98),14*.7f*zoom))using(var juice=new Pen(Color.FromArgb(255,77,151),8*.7f*zoom))using(var core=new Pen(Color.FromArgb(255,207,235),3*.7f*zoom)){
                g.DrawLine(outer,start,end);g.DrawLine(juice,start,end);g.DrawLine(core,start,end);
            }
            // Droplets along the stream make the beam read as continuous juice.
            using(var drops=new SolidBrush(Color.FromArgb(255,105,166))){
                for(int i=1;i<=6;i++){float t=(float)((i/7.0+age*.4)%1),x=start.X+(end.X-start.X)*t,y=start.Y+(end.Y-start.Y)*t;g.FillRectangle(drops,x-2*zoom,y+(i%2==0?6:-10)*zoom,4*zoom,4*zoom);}
            }
        }
    }
}
