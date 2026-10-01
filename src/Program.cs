using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GuMaGoChi {
    static class Program {
        [STAThread] static void Main(string[] args) {
            if(args.Contains("--self-test")) {try {string report=Tests.Run();File.WriteAllText(Path.Combine(Paths.BaseDirectory,"test-results.txt"),report);Environment.Exit(0);}catch(Exception ex){File.WriteAllText(Path.Combine(Paths.BaseDirectory,"test-results.txt"),ex.ToString());Environment.Exit(1);}return;}
            if(args.Contains("--render-check")) {RenderCheck.Run();return;}
            if(args.Contains("--ui-check")) {try{File.WriteAllText(Path.Combine(Paths.BaseDirectory,"ui-results.txt"),UiCheck.Run());Environment.Exit(0);}catch(Exception ex){File.WriteAllText(Path.Combine(Paths.BaseDirectory,"ui-results.txt"),ex.ToString());Environment.Exit(1);}return;}
            bool created;using(var mutex=new Mutex(true,"Local\\GuMaGoChi-Desktop",out created)) {
                if(!created){MessageBox.Show("고구마가 이미 실행 중이에요. 작업표시줄 오른쪽 트레이 아이콘을 확인해 주세요.","GuMaGoChi");return;}
                Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException+=(s,e)=>MessageBox.Show(e.Exception.Message,"GuMaGoChi 오류",MessageBoxButtons.OK,MessageBoxIcon.Error);
                try {string warning;var data=Storage.Load(out warning);Application.Run(new DesktopApp(data,warning));}
                catch(Exception ex){MessageBox.Show(ex.Message,"GuMaGoChi를 시작할 수 없습니다",MessageBoxButtons.OK,MessageBoxIcon.Error);}
            }
        }
    }
    static class Tests {
        static int count=0;
        static void Check(bool condition,string message) {if(!condition)throw new Exception("FAIL: "+message);count++;}
        public static string Run() {
            var data=new SaveData();var engine=new Engine(data,42);Pet p=engine.Adopt("첫고구마",true);
            Check(p!=null&&data.Seeds==0,"Free first adoption");p.Hunger=70;engine.Feed(p,false);Check(p.Hunger==60&&p.FoodCooldown==300,"Food subtracts 10 and starts cooldown");
            data.Dew=1;engine.Feed(p,true);Check(data.Dew==1&&p.Hunger==60,"Shared food cooldown preserves inventory");
            p.FoodCooldown=0;p.Hunger=20;engine.Feed(p,true);Check(p.Hunger==0&&data.Dew==0,"Strong food clamps to zero");
            p.FoodCooldown=0;data.Dew=1;engine.Feed(p,true);Check(data.Dew==1&&p.FoodCooldown==0,"Full pet cannot consume food");
            p.Illness=25;data.Medicine=1;engine.Treat(p,true);Check(p.Illness==0&&data.Medicine==0,"Medicine subtracts and cures at zero");
            p.Illness=50;engine.Treat(p,false);Check(p.Illness==50,"Medicine cooldown enforced");
            p.Active=false;double before=p.Age;engine.Tick(60);Check(p.Age==before&&p.MedicineCooldown==600,"Inactive freezes time and cooldowns");
            p.Active=true;p.Illness=0;p.Sleeping=true;p.Fatigue=80;engine.Tick(60);Check(p.Age==60&&p.Fatigue<80,"Sleep contributes age and recovers fatigue");
            p.Sleeping=false;p.Age=Engine.AdultAge-1;engine.Tick(5);Check(p.Age==Engine.AdultAge&&p.GrowthReady&&p.PendingSpecies>=0,"Growth exact boundary and fixed pending result");
            int pending=p.PendingSpecies;double hunger=p.Hunger;engine.Tick(1000);Check(p.Age==Engine.AdultAge&&p.Hunger==hunger&&p.PendingSpecies==pending,"Growth waiting freezes states and result");
            var decoded=Storage.Decode(Storage.Encode(data));Check(decoded.Pets[0].PendingSpecies==pending&&decoded.Pets[0].GrowthReady,"Pending growth survives serialization");
            engine.Reveal(p);Check(p.SpeciesId==pending&&!p.GrowthReady&&data.Discovered.Contains(pending),"Reveal registers discovery");
            p.Training=12;Check(!engine.Candidates(p).Any(s=>s.Id==6),"High training excludes lazy adult");
            Check(engine.Candidates(p).Count>0,"Candidates remain available");
            p.Training=0;p.TrainCooldown=0;int seeds=data.Seeds;engine.FinishActivity(p,true,2,3);Check(p.Training==1&&data.Seeds==seeds+10,"Completed training rewards once");
            engine.FinishActivity(p,true,3,3);Check(p.Training==1&&data.Seeds==seeds+10,"Repeated training cannot farm growth rewards");
            p.Waste=2;p.Dirt=70;engine.Clean(p);Check(p.Waste==1&&p.Dirt==50,"Cleaning is per waste");
            p.Age=Engine.Life-1;p.Illness=0;p.Hunger=0;p.Dirt=0;p.Fatigue=0;engine.Tick(5);Check(p.Dead&&p.Age==Engine.Life&&p.Cause=="자연사","Natural death fixed at 100 hours");
            data.Seeds=0;var rescue=engine.Adopt("새싹");Check(rescue!=null,"Rescue when all dead and no funds");
            Check(engine.Adopt("불가")==null,"Insufficient seeds cannot adopt extra pet");
            data.Seeds=100;engine.Adopt("둘째");Check(data.Seeds==0,"Paid adoption consumes seeds");
            data.Seeds=8;Check(engine.Buy(false)&&data.Seeds==0&&data.Dew==2,"Shop and shared inventory");
            // Basic meals every two hours plus direct cleaning fund an adoption by first growth.
            var economy=new SaveData();var basic=new Engine(economy,5);var baby=basic.Adopt("기본돌봄",true);
            for(int hour=0;hour<8;hour++){for(int minute=0;minute<60;minute++){basic.Tick(60);while(baby.Waste>0&&!baby.GrowthReady)basic.Clean(baby);}if(hour%2==1&&!baby.GrowthReady)basic.Feed(baby,false);}
            basic.Reveal(baby);Check(economy.Seeds>=Engine.AdoptPrice,"Basic care funds another baby around eight hours");
            var untouched=new SaveData();var untouchedEngine=new Engine(untouched,1);var untouchedPet=untouchedEngine.Adopt("기본후보",true);Check(untouchedEngine.Candidates(untouchedPet).Count==30,"Low interaction retains basic candidates");
            untouchedPet.Training=100;for(int i=0;i<1000;i++)if(untouchedEngine.ChooseSpecies(untouchedPet)==6)throw new Exception("Excluded species selected");Check(true,"Excluded species never selected in repeated draws");
            bool rejected=false;try{Storage.Decode("{\"Version\":2}");}catch{rejected=true;}Check(rejected,"Invalid saves rejected");
            var disease=new SaveData();var sickEngine=new Engine(disease);var sick=sickEngine.Adopt("아픈고구마",true);sick.Illness=99.99;sick.Hunger=100;sickEngine.Tick(60);Check(sick.Dead&&sick.Cause=="질병","Disease can kill before lifespan");
            return "PASS: "+count+" checks\r\nGrowth, lifespan, inventory, cooldowns, pause, sleep, filtering, rewards, adoption and save validation verified.\r\n";
        }
    }
    static class UiCheck {
        static void Check(bool condition,string label) {if(!condition)throw new Exception(label);}
        static ToolStripMenuItem Find(ContextMenuStrip menu,string prefix) {return menu.Items.OfType<ToolStripMenuItem>().First(i=>i.Text.StartsWith(prefix));}
        public static string Run() {
            Application.EnableVisualStyles();var data=new SaveData {Seeds=150,Dew=1,Medicine=1};var pet=new Pet {Name="테스트",Hunger=70};data.Pets.Add(pet);
            using(var app=new DesktopApp(data,null,false))try {
                Application.DoEvents();var w=app.Windows[pet.Id];Check(w.Visible&&w.TopMost&&w.TransparencyKey==System.Drawing.Color.Magenta,"Transparent topmost pet window");
                int initialX=w.Left;w.Say("이동 중에도 말해요");w.Step(.5,true);Check(w.Left!=initialX,"Autonomous walking starts immediately and continues during speech");
                var down=typeof(PetWindow).GetMethod("Down",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var up=typeof(PetWindow).GetMethod("Up",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                down.Invoke(w,new object[]{w,new MouseEventArgs(MouseButtons.Left,1,60,110,0)});Check(w.Capture,"Direct body press captures pointer without move menu");initialX=w.Left;w.Step(.5,true);Check(w.Left==initialX,"Holding body suspends autonomous walking");up.Invoke(w,new object[]{w,new MouseEventArgs(MouseButtons.Left,1,60,110,0)});Check(!w.Capture,"Releasing body releases pointer capture");
                var closingMenu=app.MenuFor(pet);closingMenu.Show(w,new System.Drawing.Point(30,90));Application.DoEvents();closingMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
                Check(!closingMenu.IsDisposed,"Menu survives Closed while item-click dispatch is pending");
                Find(closingMenu,"쓰다듬기").PerformClick();Check(pet.Affection==1,"Item action still runs after menu closes");
                var replacementMenu=app.MenuFor(pet);Check(closingMenu.IsDisposed&&!replacementMenu.IsDisposed,"Previous menu released on next opening");
                double hungerBefore=pet.Hunger;
                using(var menu=app.MenuFor(pet)){var food=Find(menu,"먹이 주기");((ToolStripMenuItem)food.DropDownItems[0]).PerformClick();}Check(Math.Abs(pet.Hunger-(hungerBefore-10))<.0001&&pet.FoodCooldown==300,"Context-menu feeding");
                using(var menu=app.MenuFor(pet))Find(menu,"집에서 재우기").PerformClick();Check(pet.Home&&pet.Sleeping&&!w.Visible&&app.Home.Visible,"Sleep opens home and hides desktop pet");
                using(var menu=app.MenuFor(pet))Find(menu,"깨우기").PerformClick();Check(!pet.Sleeping&&pet.Home,"Wake stays at home");
                using(var menu=app.MenuFor(pet))Find(menu,"외출하기").PerformClick();Check(!pet.Home&&w.Visible,"Outing restores desktop pet");
                app.Home.RefreshData();var petList=app.Home.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<ListBox>().Single();Check(petList.GetItemText(petList.Items[0]).Contains(pet.Name),"Pet list displays name without private-type binding");
                var habitat=app.Home.Controls.OfType<TabControl>().Single().TabPages[0].Controls.OfType<Habitat>().Single();Check(habitat.Preview==pet&&!pet.Home,"Outside selected pet has home preview without moving residence");
                using(var menu=app.MenuFor(pet))Find(menu,"비활성화").PerformClick();Check(!pet.Active&&!w.Visible,"Deactivation hides pet");
                using(var menu=app.MenuFor(pet))Find(menu,"활성화하기").PerformClick();Check(pet.Active&&w.Visible,"Activation restores pet");
                app.ManualPause=true;app.SyncWindows();Check(!w.Visible,"Global pause hides pet");app.ManualPause=false;app.SyncWindows();
                app.StartActivity(pet,true);Check(app.Activity!=null&&app.Activity.Visible,"Training overlay opens");app.Activity.SetPaused(true);app.Activity.SetPaused(false);app.Activity.CancelActivity();Check(app.Activity==null,"Activity closes cleanly");
                pet.Age=Engine.AdultAge-1;app.Engine.Tick(1);int pending=pet.PendingSpecies;app.Reveal(pet);Check(pet.SpeciesId==pending&&!pet.GrowthReady,"Growth reveals predetermined adult");
                var fileNames=new[]{"00.png","29.png"};Check(fileNames.All(file=>File.Exists(Path.Combine(Paths.BaseDirectory,"assets","higgsfield","characters",file))),"Packaged character assets");
                Check(Sprites.Home!=null,"Underground home background packaged");
                using(var bmp=new System.Drawing.Bitmap(120,120))using(var g=System.Drawing.Graphics.FromImage(bmp))foreach(string key in new[]{"stand","walk","eat","throw","burrow"}) {
                    g.Clear(System.Drawing.Color.Transparent);Check(Sprites.Draw(g,new System.Drawing.Rectangle(0,0,120,120),key,.625,false),"Sprite loaded: "+key);
                    bool visible=false;for(int y=0;y<120;y++)for(int x=0;x<120;x++){int alpha=bmp.GetPixel(x,y).A;if(alpha>0)visible=true;if(alpha!=0&&alpha!=255)throw new Exception("Sprite halo alpha was not removed");}Check(visible,"Sprite contains visible silhouette: "+key);
                }
                for(int id=0;id<30;id++)using(var first=new System.Drawing.Bitmap(120,120))using(var next=new System.Drawing.Bitmap(120,120))using(var a=System.Drawing.Graphics.FromImage(first))using(var b=System.Drawing.Graphics.FromImage(next)) {
                    Check(Art.ImageFor(id)!=null,"Adult asset: "+id);
                    foreach(string key in new[]{"walk","eat","throw","sleep","burrow"}) {
                        a.Clear(System.Drawing.Color.Transparent);b.Clear(System.Drawing.Color.Transparent);var p=new Pet {SpeciesId=id,Sleeping=key=="sleep",GrowthReady=key=="burrow"};
                        var box=new System.Drawing.Rectangle(0,0,120,120);Art.Pet(a,p,box,0,key);Art.Pet(b,p,box,.875,key);
                        bool changed=false;for(int y=0;y<120&&!changed;y++)for(int x=0;x<120;x++)if(first.GetPixel(x,y)!=next.GetPixel(x,y)){changed=true;break;}Check(changed,"Adult animated: "+id+" / "+key);
                    }
                }
                return "PASS: 212 UI checks\r\nWindows, pet list names, selected preview, direct drag capture, autonomous walking, menus, activities, home, baby clips and all 30 adult action renders verified.\r\n";
            }finally {app.Exit();}
        }
    }
    static class RenderCheck {
        public static void Run() {
            Application.EnableVisualStyles();var data=new SaveData {Seeds=180,Dew=3,Medicine=2};
            data.Pets.Add(new Pet {Name="밤밤이",SpeciesId=10,Home=true,Age=36000,Waste=1});data.Pets.Add(new Pet {Name="새싹",Home=true,Sleeping=true});data.Discovered.Add(10);
            using(var app=new DesktopApp(data,null,false)) {
                var home=new HomeWindow(app);home.Show();Application.DoEvents();using(var bmp=new System.Drawing.Bitmap(home.Width,home.Height)){home.DrawToBitmap(bmp,new System.Drawing.Rectangle(0,0,home.Width,home.Height));bmp.Save(Path.Combine(Paths.BaseDirectory,"home-preview.png"));}
                using(var bmp=new System.Drawing.Bitmap(900,180))using(var g=System.Drawing.Graphics.FromImage(bmp)) {g.Clear(Art.Cream);Art.Pet(g,new Pet(),new System.Drawing.Rectangle(10,10,150,150),0);Art.Pet(g,new Pet {SpeciesId=23},new System.Drawing.Rectangle(190,10,150,150),0);Art.Pet(g,new Pet {GrowthReady=true},new System.Drawing.Rectangle(370,10,150,150),0);Art.Pet(g,new Pet {SpeciesId=10,Sleeping=true},new System.Drawing.Rectangle(550,10,150,150),0);Art.Waste(g,new System.Drawing.Rectangle(780,80,40,30));bmp.Save(Path.Combine(Paths.BaseDirectory,"sprites-preview.png"));}
                using(var bmp=new System.Drawing.Bitmap(840,1800))using(var g=System.Drawing.Graphics.FromImage(bmp)) {
                    g.Clear(Art.Cream);string[] keys={"stand","walk","eat","throw","sleep","burrow"};
                    for(int id=0;id<30;id++)for(int k=0;k<keys.Length;k++){int x=(id%2)*420+k*70,y=(id/2)*120;Art.Pet(g,new Pet {SpeciesId=id,Sleeping=keys[k]=="sleep",GrowthReady=keys[k]=="burrow"},new System.Drawing.Rectangle(x,y+22,65,85),.875,keys[k]);g.DrawString(id.ToString("00")+" "+keys[k],System.Drawing.SystemFonts.DefaultFont,System.Drawing.Brushes.Black,x,y);}
                    bmp.Save(Path.Combine(Paths.BaseDirectory,"adult-actions-preview.png"));
                }
                home.Dispose();app.Exit(); // Render mode deliberately does not persist fixture data.
            }
        }
    }
}
