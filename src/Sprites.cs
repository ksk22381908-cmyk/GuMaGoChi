using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GuMaGoChi {
    public static class Sprites {
        class Clip {public Bitmap[] Frames;public int MaxWidth,MaxHeight;}
        static Dictionary<string,Clip> clips=new Dictionary<string,Clip>();static Image home;
        public static Image Home {get {if(home==null){string p=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","home","underground-home.png");if(File.Exists(p))using(var source=Image.FromFile(p))home=new Bitmap(source);}return home;}}
        static Clip Load(string key,int species=-1) {
            string cacheKey=species+":"+key;Clip cached;if(clips.TryGetValue(cacheKey,out cached))return cached;
            string file=key=="stand"?"baby-reference.png":key+"-sheet.png";
            string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","baby",file);
            if(species>=0)path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield",key=="stand"?"adults-v2":"adult-actions",species.ToString("00")+(key=="stand"?".png":"-sheet.png"));
            if(key=="sleep")path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","sleep",species<0?"baby-sheet.png":species.ToString("00")+"-sheet.png");
            if(!File.Exists(path))return null;
            using(var source=new Bitmap(path)) {
                int columns=key=="stand"?1:key=="sleep"?2:4,count=key=="sleep"?4:species>=0&&key!="stand"?4:columns*columns,w=source.Width/columns,h=source.Height/columns;
                int row=species>=0?(key=="eat"?1:key=="throw"?2:key=="burrow"?3:0):0;
                var clip=new Clip {Frames=new Bitmap[count]};
                for(int i=0;i<count;i++) {
                    using(var cell=source.Clone(new Rectangle((i%columns)*w,(row+i/columns)*h,w,h),PixelFormat.Format32bppArgb))clip.Frames[i]=Cutout(cell,key=="burrow");
                    clip.MaxWidth=Math.Max(clip.MaxWidth,clip.Frames[i].Width);clip.MaxHeight=Math.Max(clip.MaxHeight,clip.Frames[i].Height);
                }clips[cacheKey]=clip;return clip;
            }
        }
        // Binary alpha avoids blending generated halos onto the magenta color-key
        // window. Remove isolated pixels and align actual silhouettes at the feet.
        public static Bitmap Cutout(Bitmap cell,bool removeTopFragment=false) {
            int w=cell.Width,h=cell.Height;var mask=new bool[w*h];var pixels=new int[w*h];
            var data=cell.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);cell.UnlockBits(data);
            for(int i=0;i<pixels.Length;i++)mask[i]=((uint)pixels[i]>>24)>=230;
            var visited=new bool[w*h];var groups=new List<List<int>>();int largest=0;
            for(int i=0;i<mask.Length;i++)if(mask[i]&&!visited[i]) {
                var group=new List<int>();var queue=new Queue<int>();queue.Enqueue(i);visited[i]=true;
                while(queue.Count>0){int n=queue.Dequeue();group.Add(n);int x=n%w,y=n/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;int next=yy*w+xx;if(mask[next]&&!visited[next]){visited[next]=true;queue.Enqueue(next);}}}
                groups.Add(group);largest=Math.Max(largest,group.Count);
            }
            int left=w,top=h,right=0,bottom=0;var keep=new bool[w*h];
            foreach(var group in groups){
                int groupTop=h,groupBottom=0;foreach(int i in group){groupTop=Math.Min(groupTop,i/w);groupBottom=Math.Max(groupBottom,i/w);}
                // Generated burrow rows spill the previous mound into the top of
                // the next cell. Keep the central sprite and its separate sprout.
                if(removeTopFragment&&groupTop==0&&groupBottom<h/3&&group.Count<largest)continue;
                if(group.Count>=Math.Max(12,largest*.035))foreach(int i in group){keep[i]=true;left=Math.Min(left,i%w);top=Math.Min(top,i/w);right=Math.Max(right,i%w);bottom=Math.Max(bottom,i/w);}
            }
            if(left>right)return new Bitmap(1,1);
            var result=new Bitmap(right-left+1,bottom-top+1,PixelFormat.Format32bppArgb);
            for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)if(keep[y*w+x])result.SetPixel(x-left,y-top,Color.FromArgb(255,Color.FromArgb(pixels[y*w+x])));
            return result;
        }
        public static Image AdultStand(int species){Clip clip=Load("stand",species);return clip==null?null:clip.Frames[0];}
        public static bool Draw(Graphics g,Rectangle box,string key,double time,bool flip,int species=-1) {
            Clip clip=Load(key,species);if(clip==null)return false;
            bool once=key=="eat"||key=="throw"||key=="burrow";
            int frame=(int)(Math.Max(0,time)*(key=="sleep"?2:species>=0?4:8));frame=once?Math.Min(clip.Frames.Length-1,frame):frame%clip.Frames.Length;
            Bitmap image=clip.Frames[frame];float scale=Math.Min(box.Width/(float)clip.MaxWidth,box.Height/(float)clip.MaxHeight);
            int width=Math.Max(1,(int)(image.Width*scale)),height=Math.Max(1,(int)(image.Height*scale));int x=box.Left+(box.Width-width)/2,y=box.Bottom-height;
            var state=g.Save();g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;if(flip){g.TranslateTransform(x+width,y);g.ScaleTransform(-1,1);g.DrawImage(image,new Rectangle(0,0,width,height));}else g.DrawImage(image,new Rectangle(x,y,width,height));g.Restore(state);return true;
        }
        public static void Dispose() {foreach(var c in clips.Values)foreach(var f in c.Frames)f.Dispose();clips.Clear();if(home!=null)home.Dispose();home=null;}
    }
}
