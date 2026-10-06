using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuMaGoChi {
    public class ReadableMenuRenderer:ToolStripProfessionalRenderer {
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){int split=e.Text.IndexOf(" · ");if(!(e.Item is ToolStripMenuItem)||split<0){base.OnRenderItemText(e);return;}string label=e.Text.Substring(0,split),value=e.Text.Substring(split+3);int width=TextRenderer.MeasureText(label,e.TextFont).Width;Rectangle left=e.TextRectangle;left.Width=Math.Min(left.Width,width+8);Rectangle right=e.TextRectangle;right.X=left.Right+16;right.Width=Math.Max(0,e.TextRectangle.Right-right.X);Color color=e.Item.Enabled?e.TextColor:SystemColors.GrayText;TextRenderer.DrawText(e.Graphics,label,e.TextFont,left,color,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);TextRenderer.DrawText(e.Graphics,value,e.TextFont,right,e.Item.Enabled?Color.FromArgb(106,111,95):SystemColors.GrayText,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);}
    }
    public static class UiLayout {
        public static Button DialogButton(string text){return new Button {Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(100,44),Padding=new Padding(16,8,16,8),Margin=new Padding(5),UseVisualStyleBackColor=true};}
        public static FlowLayoutPanel DialogActions(params Button[] buttons){var panel=new FlowLayoutPanel {Dock=DockStyle.Bottom,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.RightToLeft,WrapContents=true,Padding=new Padding(10)};foreach(var button in buttons)panel.Controls.Add(button);return panel;}
    }
}
