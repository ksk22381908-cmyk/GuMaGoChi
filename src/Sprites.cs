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
            if(species==30){LoadSpy();return clips.TryGetValue(cacheKey,out cached)?cached:null;}
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
        static Clip LoadEvolution(string key,int id) {
            string cacheKey="evolution:"+id+":"+key;Clip cached;if(clips.TryGetValue(cacheKey,out cached))return cached;
            string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","evolutions",Evolutions.All[id].Asset+"-atlas.png");
            if(!File.Exists(path))return null;
            string[] keys={"stand","walk","eat","throw","sleep","burrow"};int maxWidth=0,maxHeight=0;
            using(var source=new Bitmap(path)) {
                var rows=EvolutionRows(source);
                string correctedPath=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","evolutions","horangoma-walk-corrected.png");
                using(var corrected=id==2?new Bitmap(correctedPath):null) {
                var correctedRows=corrected==null?null:EvolutionRows(corrected);
                for(int row=0;row<6;row++) {
                var clip=new Clip {Frames=new Bitmap[4]};int y=rows[row].Top,bottom=rows[row].Bottom;
                for(int col=0;col<4;col++) {
                    // The generated Chiwama throw starts with a leftover drinking cup.
                    // Reuse its clean wind-up frame rather than display the cup during a throw.
                    int sourceCol=id==5&&row==3&&col==0?1:col;
                    int x=sourceCol*source.Width/4,right=(sourceCol+1)*source.Width/4;
                    using(var cell=source.Clone(new Rectangle(x,y,right-x,bottom-y),PixelFormat.Format32bppArgb)) {
                        if(corrected!=null&&row==1) {
                            int cx=col*corrected.Width/4,cr=(col+1)*corrected.Width/4;
                            using(var walk=corrected.Clone(new Rectangle(cx,correctedRows[1].Top,cr-cx,correctedRows[1].Height),PixelFormat.Format32bppArgb)) {
                                RemoveWhiteBackground(walk);using(var cut=Cutout(walk)) {
                                    float scale=source.Width/(float)corrected.Width;
                                    clip.Frames[col]=new Bitmap(Math.Max(1,(int)Math.Round(cut.Width*scale)),Math.Max(1,(int)Math.Round(cut.Height*scale)),PixelFormat.Format32bppArgb);
                                    using(var g=Graphics.FromImage(clip.Frames[col])){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;g.DrawImage(cut,new Rectangle(Point.Empty,clip.Frames[col].Size));}
                                }
                            }
                        }else{RemoveWhiteBackground(cell);clip.Frames[col]=Cutout(cell,row==5);}
                    }
                    maxWidth=Math.Max(maxWidth,clip.Frames[col].Width);maxHeight=Math.Max(maxHeight,clip.Frames[col].Height);
                }
                clips["evolution:"+id+":"+keys[row]]=clip;
                }
                }
            }
            // One scale for the whole character, including sleeping and burrowing.
            foreach(string pose in keys){clips["evolution:"+id+":"+pose].MaxWidth=maxWidth;clips["evolution:"+id+":"+pose].MaxHeight=maxHeight;}
            return clips.TryGetValue(cacheKey,out cached)?cached:null;
        }
        static bool WhitePixel(int pixel){Color c=Color.FromArgb(pixel);return c.A<230||(c.R>=225&&c.G>=225&&c.B>=225&&Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))<25);}
        static bool BackgroundPixel(int pixel){Color c=Color.FromArgb(pixel);return c.A<230||(c.R>=180&&c.G>=180&&c.B>=180&&Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))<45);}
        static Rectangle[] EvolutionRows(Bitmap source) {
            int w=source.Width,h=source.Height;var pixels=new int[w*h];var bands=new List<Rectangle>();
            var data=source.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);source.UnlockBits(data);
            int top=-1,last=-1;
            for(int y=0;y<h;y++) {
                int ink=0;for(int x=0;x<w;x++)if(!WhitePixel(pixels[y*w+x]))ink++;
                if(ink>5){if(top<0)top=y;last=y;}
                if(top>=0&&(y-last>8||y==h-1)) {
                    int start=Math.Max(0,top-3),end=Math.Min(h,last+4);bands.Add(new Rectangle(0,start,w,end-start));top=-1;
                }
            }
            if(bands.Count!=6)throw new InvalidDataException("2차 진화 시트의 6개 동작 행을 확인할 수 없습니다.");
            return bands.ToArray();
        }
        // Flood only the exterior so white teeth, highlights and cups are preserved.
        static void RemoveWhiteBackground(Bitmap cell) {
            int w=cell.Width,h=cell.Height;var pixels=new int[w*h];var removed=new bool[w*h];var queue=new Queue<int>();
            var data=cell.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);
            for(int i=0;i<pixels.Length;i++)if((i/w==0||i/w==h-1||i%w==0||i%w==w-1)&&BackgroundPixel(pixels[i])){removed[i]=true;queue.Enqueue(i);}
            while(queue.Count>0){int n=queue.Dequeue(),x=n%w,y=n/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;int v=yy*w+xx;if(!removed[v]&&BackgroundPixel(pixels[v])){removed[v]=true;queue.Enqueue(v);}}}
            for(int i=0;i<pixels.Length;i++)if(removed[i])pixels[i]=0;
            for(int y=0;y<h;y++)Marshal.Copy(pixels,y*w,IntPtr.Add(data.Scan0,y*data.Stride),w);cell.UnlockBits(data);
        }
        public static Image EvolutionStand(int id){Clip clip=LoadEvolution("stand",id);return clip==null?null:clip.Frames[0];}
        static void LoadSpy(){
            string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","gancheopma","actions-atlas-draft.png");if(!File.Exists(path))return;
            string[] keys={"stand","walk","eat","throw","sleep","burrow"};int maxWidth=0,maxHeight=0;
            using(var source=new Bitmap(path))for(int row=0;row<6;row++){
                var clip=new Clip {Frames=new Bitmap[4]};int y=row*source.Height/6,bottom=(row+1)*source.Height/6;
                for(int col=0;col<4;col++){int x=col*source.Width/4,right=(col+1)*source.Width/4;using(var cell=source.Clone(new Rectangle(x,y,right-x,bottom-y),PixelFormat.Format32bppArgb))clip.Frames[col]=Cutout(cell,row==5);maxWidth=Math.Max(maxWidth,clip.Frames[col].Width);maxHeight=Math.Max(maxHeight,clip.Frames[col].Height);}
                clips["30:"+keys[row]]=clip;
            }
            foreach(string key in keys){clips["30:"+key].MaxWidth=maxWidth;clips["30:"+key].MaxHeight=maxHeight;}
        }
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
        public static bool Draw(Graphics g,Rectangle box,string key,double time,bool flip,int species=-1,int evolution=-1) {
            Clip clip=evolution>=0?LoadEvolution(key,evolution):Load(key,species);if(clip==null)return false;
            bool once=key=="eat"||key=="throw"||key=="burrow";
            int frame=(int)(Math.Max(0,time)*(key=="sleep"?2:species>=0?4:8));frame=once?Math.Min(clip.Frames.Length-1,frame):frame%clip.Frames.Length;
            Bitmap image=clip.Frames[frame];float scale=Math.Min(box.Width/(float)clip.MaxWidth,box.Height/(float)clip.MaxHeight);
            int width=Math.Max(1,(int)(image.Width*scale)),height=Math.Max(1,(int)(image.Height*scale));int x=box.Left+(box.Width-width)/2,y=box.Bottom-height;
            var state=g.Save();g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;if(flip){g.TranslateTransform(x+width,y);g.ScaleTransform(-1,1);g.DrawImage(image,new Rectangle(0,0,width,height));}else g.DrawImage(image,new Rectangle(x,y,width,height));g.Restore(state);return true;
        }
        public static void Dispose() {foreach(var c in clips.Values)foreach(var f in c.Frames)f.Dispose();clips.Clear();if(home!=null)home.Dispose();home=null;}
    }
}
