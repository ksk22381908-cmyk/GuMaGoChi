using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace GuMaGoChi {
    public static class BabySpriteTests {
        static int count;
        static void Check(bool condition,string name){if(!condition)throw new Exception("Baby sprites: "+name);count++;}
        public static int Run(){
            count=0;string[] keys={"stand","walk","eat","throw","sleep","burrow"};
            using(var sheet=new Bitmap(Path.Combine(Paths.BaseDirectory,"assets","higgsfield","baby","baby-b-atlas.png"))){
                Check(sheet.Width%4==0,"atlas has four equal columns");
                var rows=(Rectangle[])typeof(Sprites).GetMethod("EvolutionRows",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{sheet});
                Check(rows.Length==6,"atlas has six separate motion rows");
            }
            using(var preview=new Bitmap(4*160,6*170,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview)){
                g.Clear(Art.Cream);
                for(int row=0;row<keys.Length;row++)for(int frame=0;frame<4;frame++){
                    double time=frame*(keys[row]=="stand"||keys[row]=="sleep"?.5:.25);
                    using(var image=new Bitmap(128,128,PixelFormat.Format32bppArgb))using(var draw=Graphics.FromImage(image)){
                        Check(Sprites.Draw(draw,new Rectangle(0,0,128,128),keys[row],time,false),"loads "+keys[row]+" frame "+frame);
                        int pixels=0,left=128,right=0,top=128,bottom=0;
                        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                            Color p=image.GetPixel(x,y);Check(p.A==0||p.A==255,"binary alpha on magenta desktop");
                            if(p.A>0){pixels++;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                        }
                        Check(pixels>100,"nonempty "+keys[row]+" frame "+frame);
                        Check(left>0&&right<127&&top>0&&bottom<128,"sprite remains inside bounds");
                        if(row==5&&frame==3)Check(bottom-top<60,"final evolution frame is a low mound");
                        g.DrawImageUnscaled(image,frame*160+16,row*170+24);
                        g.DrawString(keys[row]+" "+(frame+1),SystemFonts.DefaultFont,Brushes.SaddleBrown,frame*160+12,row*170+4);
                    }
                }
                preview.Save(Path.Combine(Paths.BaseDirectory,"baby-b-motion-preview.png"),ImageFormat.Png);
            }
            return count;
        }
    }
}
