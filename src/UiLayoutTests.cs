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
            CheckZoomText();
            CheckEmergencyExit();
            CheckDisplayScale();
            foreach(float scale in new[]{1f,1.25f,1.5f,2f}){using(var dialog=new NameDialog("새 아기 고구마의 이름",""))CheckButtons(dialog,scale,"adoption-preview-"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");using(var dialog=new LunchMenuEditor(new List<string>()))CheckButtons(dialog,scale,"menu-editor-preview-"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");}
            var data=new SaveData {Seeds=442};for(int i=0;i<8;i++)data.Pets.Add(new Pet {Name="테스트 고구마 "+(i+1),SpeciesId=i,Home=true,Age=36000,GrowthExp=180,Waste=1});
            using(var app=new DesktopApp(data,null,false))try{using(var home=new HomeWindow(app)){home.Show();Application.DoEvents();var tab=home.Controls.OfType<TabControl>().Single();var habitat=tab.TabPages[0].Controls.OfType<Habitat>().Single();Check(habitat.PageCount==2&&habitat.VisibleResidents.Length==6,"first page shows six residents");for(int i=0;i<6;i++){var slot=habitat.Slot(i);Check(habitat.ClientRectangle.Contains(new Rectangle(slot.X-15,slot.Y,slot.Width+30,slot.Height+64)),"sprite, name and cleanup fit for slot "+i);for(int j=i+1;j<6;j++)Check(!slot.IntersectsWith(habitat.Slot(j)),"six sprite slots do not overlap");}using(var bmp=new Bitmap(home.Width,home.Height)){home.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(Path.Combine(Paths.BaseDirectory,"home-six-preview.png"));}habitat.SetPage(1);home.RefreshData();Check(habitat.Page==1&&habitat.VisibleResidents.Length==2,"refresh preserves second page");home.SelectPet(data.Pets[0].Id);Check(habitat.Page==0,"selecting resident opens its page");habitat.SetPage(1);data.Pets.RemoveRange(6,2);home.RefreshData();Check(habitat.Page==0&&habitat.PageCount==1,"removing last page clamps pagination");using(var menu=app.MenuFor(data.Pets[0])){Check(menu.Items.OfType<ToolStripLabel>().Any(i=>i.Text=="돌봄"),"care section heading");Check(menu.Items.OfType<ToolStripMenuItem>().Last().Text=="맛탕 만들기","destructive action stays last");}home.Dispose();}}finally{app.Exit();}
        }
        static void CheckZoomText(){
            foreach(float scale in new[]{.5f,.7f,1f,1.5f,2f})using(var actual=new Bitmap(700,120))using(var expected=new Bitmap(700,120))using(var g=Graphics.FromImage(actual))using(var reference=Graphics.FromImage(expected))using(var font=new Font("맑은 고딕",10))using(var nativeFont=new Font("맑은 고딕",10*scale)){
                g.Clear(Color.White);reference.Clear(Color.White);g.ScaleTransform(scale,scale);
                var flags=TextFormatFlags.NoPadding|TextFormatFlags.PreserveGraphicsClipping;var rect=new Rectangle(4,4,320,50);
                ZoomText.DrawText(g,"고구마 · 배고픔 100 / 100",font,rect,Color.Black,flags);
                TextRenderer.DrawText(reference,"고구마 · 배고픔 100 / 100",nativeFont,DisplayZoom.Rect(rect,scale),Color.Black,flags);
                int ink=0;for(int x=0;x<actual.Width;x++)for(int y=0;y<actual.Height;y++){Check(actual.GetPixel(x,y)==expected.GetPixel(x,y),"text rasterized directly at native size "+scale);if(actual.GetPixel(x,y).ToArgb()!=Color.White.ToArgb())ink++;}
                Check(ink>20&&Math.Abs(g.Transform.Elements[0]-scale)<.001,"scaled text remains visible without changing sprite transform");
            }
        }
        static void CheckDisplayScale(){
            var data=new SaveData {StarterNutrientsClaimed=true};var pet=new Pet {Name="배율 테스트"};data.Pets.Add(pet);
            using(var app=new DesktopApp(data,null,false)){
                app.OpenHome(null);Application.DoEvents();var home=app.Home;var window=app.Windows[pet.Id];Size original=home.ClientSize;
                var tab=home.Controls.OfType<TabControl>().Single();var habitat=tab.TabPages[0].Controls.OfType<Habitat>().Single();
                foreach(int percent in Enumerable.Range(1,20).Select(i=>i*10)){
                    app.SetDisplayScale(percent);Application.DoEvents();float factor=percent/100f;
                    Check(window.ClientSize==new Size(DisplayZoom.Pixels(126,factor),DisplayZoom.Pixels(166,factor)),"pet and speech window scale to "+percent);
                    Check(Math.Abs(home.UiZoom-factor)<.001&&home.Font.Size>0,"home UI scale applies "+percent);
                    Check(window.ClientRectangle.Contains(window.ScaledSpriteBounds)&&habitat.Slot(0).Width>0,"scaled character hit area remains valid "+percent);
                    Check(Storage.Decode(Storage.Encode(data)).DisplayScalePercent==percent,"display scale persists "+percent);
                    if(percent==10||percent==50||percent==100||percent==200)using(var image=new Bitmap(home.Width,home.Height)){home.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(Paths.BaseDirectory,"home-zoom-"+percent+".png"));}
                }
                app.SetDisplayScale(100);Check(home.ClientSize==original&&window.ClientSize==new Size(126,166),"restoring 100 percent avoids cumulative rounding");
                using(var menu=app.ScaleMenu()){Check(menu.DropDownItems.Count==20&&menu.DropDownItems.OfType<ToolStripMenuItem>().Single(i=>i.Checked).Text.StartsWith("100%"),"scale menu has twenty steps and current selection");}
                app.Exit();
            }
            Check(new Engine(new SaveData {DisplayScalePercent=-20}).Data.DisplayScalePercent==10&&new Engine(new SaveData {DisplayScalePercent=999}).Data.DisplayScalePercent==200,"invalid scales clamp safely");
        }
        static void CheckEmergencyExit(){
            foreach(bool byMenu in new[]{false,true}){
                var data=new SaveData {StarterNutrientsClaimed=true};
                var desktopPet=new Pet {Name="비상탈출 테스트",Age=120,GrowthExp=2};
                data.Pets.Add(desktopPet);data.Pets.Add(new Pet {Home=true,Sleeping=true});data.Pets.Add(new Pet {Active=false});
                using(var app=new DesktopApp(data,null,false)){
                    app.OpenHome(null);Application.DoEvents();var window=app.Windows[desktopPet.Id];var home=app.Home;
                    app.CheckEmergencyKeys(true,false);app.CheckEmergencyKeys(false,true);app.CheckEmergencyKeys(false,false);
                    Check(desktopPet.Active&&window.Visible&&!home.IsDisposed,"single keys never trigger emergency exit");
                    double ageBefore=desktopPet.Age,expBefore=desktopPet.GrowthExp;
                    if(byMenu){var menu=app.MenuFor(desktopPet);menu.Items.OfType<ToolStripMenuItem>().Single(i=>i.Text=="비상탈출 · Space + E").PerformClick();}
                    else app.CheckEmergencyKeys(true,true);
                    Check(data.Pets[0].Active&&data.Pets[1].Active&&!data.Pets[2].Active&&window.IsDisposed&&home.IsDisposed,"shortcut and right-click preserve activation while closing windows");
                    var saved=Storage.Decode(Storage.Encode(data));Check(saved.Pets[0].Active&&saved.Pets[1].Active&&!saved.Pets[2].Active&&saved.Pets[0].Age==ageBefore&&saved.Pets[0].GrowthExp==expBefore,"emergency save preserves activation and progress");
                    app.CheckEmergencyKeys(true,true);app.EmergencyExit();app.Exit();
                }
            }
        }
    }
}
