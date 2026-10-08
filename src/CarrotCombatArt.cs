using System;
using System.Drawing;

namespace GuMaGoChi {
    public static class CarrotCombatArt {
        public static bool Farmer(Pet pet){return pet.SpeciesId==26&&pet.EvolutionId==10;}
        public static bool Spitter(Pet pet){return pet.SpeciesId==26&&pet.EvolutionId==11;}
        public static bool Evolved(Pet pet){return Farmer(pet)||Spitter(pet);}
        public static Point ActivityAnchor(Pet pet){return Spitter(pet)?new Point(62,92):PetWindow.BallAnchor;}
        public static void Projectile(Graphics g,PointF point,Pet pet,float size){
            string[] pixels=Spitter(pet)?new[]{"        ","  ####  "," #oooo# "," #ohho# ","  #oo#  ","   ##   ","        ","        "}:new[]{"    gg g","    ggg ","   #### ","  #hho# "," #hooo# "," #ooo#  ","  #o#   ","   #    "};
            float unit=size/8;
            using(var outline=new SolidBrush(Color.FromArgb(62,28,12)))using(var orange=new SolidBrush(Color.FromArgb(255,132,16)))using(var light=new SolidBrush(Color.FromArgb(255,202,68)))using(var green=new SolidBrush(Color.FromArgb(100,190,28))){
                for(int y=0;y<8;y++)for(int x=0;x<8;x++){
                    char pixel=pixels[y][x];if(pixel==' ')continue;
                    g.FillRectangle(pixel=='#'?outline:pixel=='o'?orange:pixel=='h'?light:green,point.X-size/2+x*unit,point.Y-size/2+y*unit,unit,unit);
                }
            }
        }
        public static void Impact(Graphics g,double age){
            using(var orange=new SolidBrush(Color.FromArgb(255,146,30)))for(int i=0;i<5;i++){
                double angle=i*Math.PI*2/5;float radius=(float)(5+age*55);
                g.FillRectangle(orange,(float)Math.Cos(angle)*radius-2,(float)Math.Sin(angle)*radius-2,4,4);
            }
        }
    }
}
