using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace GuMaGoChi {
    public class GameForm:Form {
        readonly Icon applicationIcon;
        readonly Dictionary<Control,ZoomLayout> zoomLayouts=new Dictionary<Control,ZoomLayout>();
        Size zoomClient,zoomMinimum;Font zoomFont,scaledZoomFont;bool zoomCaptured;
        public float UiZoom {get;protected set;}
        public GameForm(){UiZoom=1;applicationIcon=Icon.ExtractAssociatedIcon(typeof(GameForm).Assembly.Location);if(applicationIcon!=null)Icon=applicationIcon;}
        protected override void OnLoad(EventArgs e){base.OnLoad(e);ApplyUiZoom(DisplayZoom.Percent);}
        protected virtual bool KeepScreenBounds {get{return FormBorderStyle==FormBorderStyle.None&&!(this is PetWindow);}}
        public virtual void ApplyUiZoom(int percent){
            float factor=DisplayZoom.Normalize(percent)/100f;
            if(!zoomCaptured){zoomCaptured=true;zoomClient=ClientSize;zoomMinimum=MinimumSize;zoomFont=Font;AutoScaleMode=AutoScaleMode.None;}
            CaptureZoom(Controls);UiZoom=factor;SuspendLayout();
            if(this is HomeWindow){AutoScroll=false;foreach(Control c in Controls)c.Anchor=AnchorStyles.Top|AnchorStyles.Left;}
            if(!KeepScreenBounds)AutoScrollPosition=Point.Empty;
            MinimumSize=Size.Empty;
            if(!KeepScreenBounds)ClientSize=new Size(DisplayZoom.Pixels(zoomClient.Width,factor),DisplayZoom.Pixels(zoomClient.Height,factor));
            var previousFont=scaledZoomFont;scaledZoomFont=new Font(zoomFont.FontFamily,Math.Max(.5f,zoomFont.Size*factor),zoomFont.Style,zoomFont.Unit);Font=scaledZoomFont;if(previousFont!=null)previousFont.Dispose();
            ApplyZoom(Controls,factor);
            if(!KeepScreenBounds){Size available=Screen.FromControl(this).WorkingArea.Size;MinimumSize=new Size(Math.Min(available.Width,DisplayZoom.Pixels(zoomMinimum.Width,factor)),Math.Min(available.Height,DisplayZoom.Pixels(zoomMinimum.Height,factor)));}
            if(this is HomeWindow){foreach(Control c in Controls)c.Anchor=factor>1?AnchorStyles.Top|AnchorStyles.Left:zoomLayouts[c].Anchor;AutoScrollMinSize=factor>1?new Size(DisplayZoom.Pixels(zoomClient.Width,factor),DisplayZoom.Pixels(zoomClient.Height,factor)):Size.Empty;AutoScroll=factor>1;}
            ResumeLayout(true);Invalidate(true);
        }
        void CaptureZoom(Control.ControlCollection controls){foreach(Control c in controls){if(!zoomLayouts.ContainsKey(c))zoomLayouts[c]=new ZoomLayout(c);CaptureZoom(c.Controls);}}
        protected void ZoomNewControls(){if(!zoomCaptured)return;ZoomNewControls(Controls);}
        void ZoomNewControls(Control.ControlCollection controls){foreach(Control c in controls){if(!zoomLayouts.ContainsKey(c)){zoomLayouts[c]=new ZoomLayout(c);zoomLayouts[c].Apply(c,UiZoom);}ZoomNewControls(c.Controls);}foreach(var c in new List<Control>(zoomLayouts.Keys))if(c.IsDisposed){zoomLayouts[c].Dispose();zoomLayouts.Remove(c);}}
        void ApplyZoom(Control.ControlCollection controls,float factor){foreach(Control c in controls){c.SuspendLayout();zoomLayouts[c].Apply(c,factor);ApplyZoom(c.Controls,factor);c.ResumeLayout(true);}}
        protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing){if(applicationIcon!=null)applicationIcon.Dispose();foreach(var layout in zoomLayouts.Values)layout.Dispose();zoomLayouts.Clear();if(scaledZoomFont!=null)scaledZoomFont.Dispose();}}
    }
    public class ReadableMenuRenderer:ToolStripProfessionalRenderer {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){int split=e.Text.IndexOf(" · ");if(!(e.Item is ToolStripMenuItem)||split<0){base.OnRenderItemText(e);return;}string label=e.Text.Substring(0,split),value=e.Text.Substring(split+3);int width=TextRenderer.MeasureText(label,e.TextFont).Width;Rectangle left=e.TextRectangle;left.Width=Math.Min(left.Width,width+8);Rectangle right=e.TextRectangle;right.X=left.Right+16;right.Width=Math.Max(0,e.TextRectangle.Right-right.X);Color color=e.Item.Enabled?e.TextColor:SystemColors.GrayText;TextRenderer.DrawText(e.Graphics,label,e.TextFont,left,color,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);TextRenderer.DrawText(e.Graphics,value,e.TextFont,right,e.Item.Enabled?Color.FromArgb(106,111,95):SystemColors.GrayText,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
    }
    public static class UiLayout {
        public static Button DialogButton(string text){return new Button {Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(100,44),Padding=new Padding(16,8,16,8),Margin=new Padding(5),UseVisualStyleBackColor=true};}
        public static FlowLayoutPanel DialogActions(params Button[] buttons){var panel=new FlowLayoutPanel {Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.RightToLeft,WrapContents=true,Padding=new Padding(10)};foreach(var button in buttons)panel.Controls.Add(button);return panel;}
    }
}
