using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GuMaGoChi {
    public static class UiLayoutTests {
        static void Check(bool condition,string name){if(!condition)throw new Exception("UI layout: "+name);}
        static IEnumerable<Button> Buttons(Control control){foreach(Control child in control.Controls){var button=child as Button;if(button!=null)yield return button;foreach(var nested in Buttons(child))yield return nested;}}
        static void CheckButtons(Form form,float scale,string file){form.Scale(new SizeF(scale,scale));form.Show();form.PerformLayout();Application.DoEvents();var buttons=Buttons(form).ToArray();foreach(var b in buttons){var text=TextRenderer.MeasureText(b.Text,b.Font);Check(b.ClientSize.Width>=text.Width+b.Padding.Horizontal&&b.ClientSize.Height>=text.Height+b.Padding.Vertical,"button text fits at "+scale+": "+b.Text);Check(b.Parent.ClientRectangle.Contains(b.Bounds),"button remains inside footer: "+b.Text);}for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)Check(!buttons[i].Bounds.IntersectsWith(buttons[j].Bounds),"dialog buttons do not overlap");using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(Paths.BaseDirectory,file));}form.Hide();}
        public static void Run(){
            foreach(float scale in new[]{1f,1.25f,1.5f,2f}){using(var dialog=new NameDialog("새 아기 고구마의 이름",""))CheckButtons(dialog,scale,"adoption-preview-"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");using(var dialog=new LunchMenuEditor(new List<string>()))CheckButtons(dialog,scale,"menu-editor-preview-"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");}
            var data=new SaveData {Seeds=442};for(int i=0;i<8;i++)data.Pets.Add(new Pet {Name="테스트 고구마 "+(i+1),SpeciesId=i,Home=true,Age=36000,GrowthExp=180,Waste=1});
            using(var app=new DesktopApp(data,null,false))try{using(var home=new HomeWindow(app)){home.Show();Application.DoEvents();var tab=home.Controls.OfType<TabControl>().Single();var habitat=tab.TabPages[0].Controls.OfType<Habitat>().Single();Check(habitat.PageCount==2&&habitat.VisibleResidents.Length==6,"first page shows six residents");for(int i=0;i<6;i++){var slot=habitat.Slot(i);Check(habitat.ClientRectangle.Contains(new Rectangle(slot.X-15,slot.Y,slot.Width+30,slot.Height+64)),"sprite, name and cleanup fit for slot "+i);for(int j=i+1;j<6;j++)Check(!slot.IntersectsWith(habitat.Slot(j)),"six sprite slots do not overlap");}using(var bmp=new Bitmap(home.Width,home.Height)){home.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(Paths.BaseDirectory,"home-six-preview.png"));}habitat.SetPage(1);home.RefreshData();Check(habitat.Page==1&&habitat.VisibleResidents.Length==2,"refresh preserves second page");home.SelectPet(data.Pets[0].Id);Check(habitat.Page==0,"selecting resident opens its page");habitat.SetPage(1);data.Pets.RemoveRange(6,2);home.RefreshData();Check(habitat.Page==0&&habitat.PageCount==1,"removing last page clamps pagination");using(var menu=app.MenuFor(data.Pets[0])){Check(menu.Items.OfType<ToolStripLabel>().Any(i=>i.Text=="돌봄"),"care section heading");Check(menu.Items.OfType<ToolStripMenuItem>().Last().Text=="맛탕 만들기","destructive action stays last");}home.Dispose();}}finally{app.Exit();}
        }
    }
}
