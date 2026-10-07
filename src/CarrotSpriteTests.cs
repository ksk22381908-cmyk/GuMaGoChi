using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GuMaGoChi {
    public static class CarrotSpriteTests {
        public static int Run(){
            int count=0;string[] poses={"stand","walk","eat","throw","sleep","burrow"};
            using(var preview=new Bitmap(640,1020))using(var g=Graphics.FromImage(preview)){
                g.Clear(Art.Cream);
                for(int row=0;row<poses.Length;row++)for(int frame=0;frame<4;frame++){
                    using(var image=new Bitmap(128,128,PixelFormat.Format32bppArgb))using(var draw=Graphics.FromImage(image)){
                        if(!Sprites.Draw(draw,new Rectangle(0,0,128,128),poses[row],frame*(row==4?.5:.25),false,26))throw new Exception("Carrot frame missing: "+poses[row]+frame);
                        int visible=0,orange=0;
                        for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                            var p=image.GetPixel(x,y);if(p.A!=0&&p.A!=255)throw new Exception("Carrot alpha halo");
                            if(p.A>0){visible++;if(p.R>p.G*1.2&&p.G>p.B*1.3)orange++;}
                        }
                        if(visible<100||((row!=5||frame<3)&&orange<10))throw new Exception("Carrot outfit or silhouette missing: "+poses[row]+frame);
                        count+=3;g.DrawImageUnscaled(image,frame*160+16,row*170+24);
                        g.DrawString(poses[row]+" "+(frame+1),SystemFonts.DefaultFont,Brushes.SaddleBrown,frame*160+12,row*170+4);
                    }
                }
                preview.Save(Path.Combine(Paths.BaseDirectory,"carrot-motion-preview.png"));
            }
            return count;
        }
    }
}
