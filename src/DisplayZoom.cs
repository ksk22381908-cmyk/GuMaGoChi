using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GuMaGoChi {
    public static class DisplayZoom {
        public static int Percent=100;
        public static int Normalize(int percent){return Math.Max(10,Math.Min(200,((percent+5)/10)*10));}
        public static int Pixels(int value,float factor){return Math.Max(value==0?0:1,(int)Math.Round(value*factor));}
        public static Rectangle Rect(Rectangle r,float factor){return new Rectangle((int)Math.Round(r.X*factor),(int)Math.Round(r.Y*factor),Pixels(r.Width,factor),Pixels(r.Height,factor));}
        public static Padding Pad(Padding p,float factor){return new Padding(Pixels(p.Left,factor),Pixels(p.Top,factor),Pixels(p.Right,factor),Pixels(p.Bottom,factor));}
    }
    internal sealed class ZoomLayout {
        internal Rectangle Bounds;internal Size Minimum,Maximum;internal Padding Padding,Margin;internal Font Font;internal Point TabPadding;internal AnchorStyles Anchor;Font scaledFont;
        internal ZoomLayout(Control c){Bounds=c.Bounds;Minimum=c.MinimumSize;Maximum=c.MaximumSize;Padding=c.Padding;Margin=c.Margin;Anchor=c.Anchor;Font=(Font)c.Font.Clone();var tab=c as TabControl;if(tab!=null)TabPadding=tab.Padding;}
        internal void Apply(Control c,float factor){
            c.MinimumSize=new Size(DisplayZoom.Pixels(Minimum.Width,factor),DisplayZoom.Pixels(Minimum.Height,factor));
            c.MaximumSize=new Size(DisplayZoom.Pixels(Maximum.Width,factor),DisplayZoom.Pixels(Maximum.Height,factor));
            c.Padding=DisplayZoom.Pad(Padding,factor);c.Margin=DisplayZoom.Pad(Margin,factor);
            var previous=scaledFont;scaledFont=new Font(Font.FontFamily,Math.Max(.5f,Font.Size*factor),Font.Style,Font.Unit);c.Font=scaledFont;if(previous!=null)previous.Dispose();
            c.Bounds=DisplayZoom.Rect(Bounds,factor);
            var tab=c as TabControl;if(tab!=null)tab.Padding=new Point(DisplayZoom.Pixels(TabPadding.X,factor),DisplayZoom.Pixels(TabPadding.Y,factor));
        }
        internal void Dispose(){Font.Dispose();if(scaledFont!=null)scaledFont.Dispose();}
    }
    public class ZoomPaintControl:Control {
        protected float Zoom {get {var form=FindForm() as GameForm;return form==null?1:form.UiZoom;}}
        protected int LogicalWidth {get{return Math.Max(1,(int)Math.Round(base.Width/Zoom));}}
        protected int LogicalHeight {get{return Math.Max(1,(int)Math.Round(base.Height/Zoom));}}
        protected Font LogicalFont;
        protected void PaintLogical(PaintEventArgs e,Action<PaintEventArgs> paint){
            using(var font=new Font(Font.FontFamily,Math.Max(.5f,Font.Size/Zoom),Font.Style,Font.Unit)){
                LogicalFont=font;
                var state=e.Graphics.Save();
                try{e.Graphics.ScaleTransform(Zoom,Zoom);paint(e);}finally{e.Graphics.Restore(state);}
            }
        }
    }
    public static class ZoomText {
        static void Draw(Graphics g,Font font,Action<Font,Matrix> paint){
            using(var matrix=g.Transform){
                float[] elements=matrix.Elements;float scale=(float)Math.Sqrt(elements[0]*elements[0]+elements[1]*elements[1]);
                using(var deviceFont=new Font(font.FontFamily,Math.Max(.5f,font.Size*scale),font.Style,font.Unit)){
                    var state=g.Save();try{g.ResetTransform();paint(deviceFont,matrix);}finally{g.Restore(state);}
                }
            }
        }
        static Rectangle Bounds(Matrix matrix,Rectangle rect){var points=new[]{new PointF(rect.Left,rect.Top),new PointF(rect.Right,rect.Bottom)};matrix.TransformPoints(points);return Rectangle.FromLTRB((int)Math.Round(points[0].X),(int)Math.Round(points[0].Y),(int)Math.Round(points[1].X),(int)Math.Round(points[1].Y));}
        static Point Position(Matrix matrix,Point point){var points=new[]{new PointF(point.X,point.Y)};matrix.TransformPoints(points);return Point.Round(points[0]);}
        // GDI text ignores Graphics scaling. Rasterize the font at its final size.
        public static void DrawText(Graphics g,string text,Font font,Rectangle bounds,Color color,TextFormatFlags flags){Draw(g,font,(f,m)=>TextRenderer.DrawText(g,text,f,Bounds(m,bounds),color,flags|TextFormatFlags.PreserveGraphicsClipping));}
        public static void DrawText(Graphics g,string text,Font font,Rectangle bounds,Color color,Color background,TextFormatFlags flags){Draw(g,font,(f,m)=>TextRenderer.DrawText(g,text,f,Bounds(m,bounds),color,background,flags|TextFormatFlags.PreserveGraphicsClipping));}
        public static void DrawText(Graphics g,string text,Font font,Point point,Color color){Draw(g,font,(f,m)=>TextRenderer.DrawText(g,text,f,Position(m,point),color));}
        public static void DrawText(Graphics g,string text,Font font,Point point,Color color,Color background){Draw(g,font,(f,m)=>TextRenderer.DrawText(g,text,f,Position(m,point),color,background));}
    }
}
