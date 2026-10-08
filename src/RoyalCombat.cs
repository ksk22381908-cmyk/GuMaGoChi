using System;
using System.Drawing;

namespace GuMaGoChi {
    public static class RoyalCombat {
        public static bool King(Pet pet){return pet.SpeciesId==10&&pet.EvolutionId==12;}
        public static bool Beggar(Pet pet){return pet.SpeciesId==10&&pet.EvolutionId==13;}
        public static bool Evolved(Pet pet){return King(pet)||Beggar(pet);}
        public static void Trash(Graphics g,PointF point,float size){
            string[] pixels={"  ###   "," #hh##  ","#hddhh# ","#hdhdh##"," #dhhdh#","  #hdd# ","   ###  ","        "};
            using(var outline=new SolidBrush(Color.FromArgb(65,48,32)))using(var paper=new SolidBrush(Color.FromArgb(203,191,154)))using(var fold=new SolidBrush(Color.FromArgb(137,119,90))){
                float unit=size/8;for(int y=0;y<8;y++)for(int x=0;x<8;x++){
                    char pixel=pixels[y][x];if(pixel!=' ')g.FillRectangle(pixel=='#'?outline:pixel=='h'?paper:fold,point.X-size/2+x*unit,point.Y-size/2+y*unit,unit,unit);
                }
            }
        }
        public static void Effect(Graphics g,DefenseEffect fx){
            bool beggar=fx.EvolutionId==13;
            if(beggar&&!fx.Skill){Trash(g,new PointF(),(float)Math.Max(4,18-fx.Age*40));return;}
            if(fx.Age<.5){
                if(beggar){
                    float y=(float)(-90*(1-fx.Age/.5));
                    using(var wood=new SolidBrush(Color.FromArgb(153,94,44)))using(var rim=new Pen(Color.FromArgb(67,38,20),3)){
                        g.FillPie(wood,-28,y-16,56,36,0,180);g.DrawArc(rim,-28,y-16,56,36,0,180);g.DrawEllipse(rim,-28,y-8,56,16);
                    }
                }else using(var anticipation=new Pen(Color.FromArgb(175,122,42),2))g.DrawEllipse(anticipation,-12,-12,24,24);
                return;
            }
            float progress=(float)Math.Min(1,(fx.Age-.5)/.4),radius=(float)fx.Radius*progress;
            using(var outer=new Pen(beggar?Color.Sienna:Color.Goldenrod,5))using(var inner=new Pen(beggar?Color.BurlyWood:Color.LightGoldenrodYellow,3)){
                g.DrawEllipse(outer,-radius,-radius,2*radius,2*radius);
                g.DrawEllipse(inner,-radius*.65f,-radius*.65f,1.3f*radius,1.3f*radius);
            }
            if(beggar)using(var shard=new SolidBrush(Color.SaddleBrown))for(int i=0;i<6;i++){
                double angle=i*Math.PI/3;float x=(float)Math.Cos(angle)*radius*.7f,y=(float)Math.Sin(angle)*radius*.7f;
                g.FillRectangle(shard,x-4,y-3,8,6);
            }
        }
    }
}
