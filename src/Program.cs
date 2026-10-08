using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GuMaGoChi {
    static class Program {
        [STAThread] static void Main(string[] args) {
            if(args.Length>0&&args[0]=="--apply-update"){Updates.Apply(args);return;}
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
            count+=DefenseTests.Run();count+=CarrotCombatTests.Run();count+=RoyalCombatTests.Run();count+=UpdateTests.Run();
            var mattangData=new SaveData {Seeds=27};var mattangEngine=new Engine(mattangData);
            var removed=mattangEngine.Adopt("맛탕",true);removed.SpeciesId=12;mattangData.Discovered.Add(12);
            var survivor=mattangEngine.Adopt("남은 고구마",true);survivor.Friends[removed.Id]=3;
            Check(mattangEngine.MakeMattang(removed)&&!mattangData.Pets.Contains(removed)&&mattangData.Pets.Contains(survivor),"Mattang deletes only selected pet");
            Check(!survivor.Friends.ContainsKey(removed.Id)&&mattangData.Discovered.Contains(12)&&mattangData.Seeds==27,"Mattang clears links and preserves discovery and currency");
            Check(!mattangEngine.MakeMattang(removed)&&!mattangEngine.MakeMattang(null),"Mattang is safe for repeated or invalid targets");
            Check(Storage.Decode(Storage.Encode(mattangData)).Pets.Count==1,"Mattang deletion persists after save roundtrip");
            var gift=new SaveData();new Engine(gift);Check(gift.Nutrients==6&&gift.StarterNutrientsClaimed,"New player receives six starter nutrients");new Engine(gift);Check(gift.Nutrients==6,"Repeated engine start does not repeat starter gift");var returning=Storage.Decode("{\"Version\":2,\"Nutrients\":4,\"Pets\":[],\"Discovered\":[]}");new Engine(returning);Check(returning.Nutrients==10&&returning.StarterNutrientsClaimed,"Existing player keeps inventory and receives six nutrients");var giftReload=Storage.Decode(Storage.Encode(returning));new Engine(giftReload);Check(giftReload.Nutrients==10,"Saved gift claim prevents repeat after restart");giftReload.Nutrients=0;new Engine(giftReload);Check(giftReload.Nutrients==0,"Consumed gift cannot be claimed again");
            var data=new SaveData();var engine=new Engine(data,42);Pet p=engine.Adopt("첫고구마",true);
            Check(p!=null&&data.Seeds==0,"Free first adoption");p.Hunger=70;engine.Feed(p,false);Check(p.Hunger==60&&p.FoodCooldown==300,"Food subtracts 10 and starts cooldown");
            data.Dew=1;engine.Feed(p,true);Check(data.Dew==1&&p.Hunger==60,"Shared food cooldown preserves inventory");
            p.FoodCooldown=0;p.Hunger=20;engine.Feed(p,true);Check(p.Hunger==0&&data.Dew==0,"Strong food clamps to zero");
            p.FoodCooldown=0;data.Dew=1;engine.Feed(p,true);Check(data.Dew==1&&p.FoodCooldown==0,"Full pet cannot consume food");
            p.Illness=25;data.Medicine=1;engine.Treat(p,true);Check(p.Illness==0&&data.Medicine==0,"Medicine subtracts and cures at zero");
            p.Illness=50;engine.Treat(p,false);Check(p.Illness==50,"Medicine cooldown enforced");
            p.Active=false;double before=p.Age;engine.Tick(60);Check(p.Age==before&&p.MedicineCooldown==600,"Inactive freezes time and cooldowns");
            p.Active=true;p.Illness=0;p.Sleeping=true;p.Fatigue=80;engine.Tick(60);Check(p.Age==60&&p.Fatigue<80,"Sleep contributes age and recovers fatigue");
            p.Sleeping=false;p.Age=Engine.AdultAge-1;p.GrowthExp=Engine.AdultExp-1.0/60;engine.Tick(5);Check(p.Age==Engine.AdultAge&&p.GrowthReady&&p.PendingSpecies>=0,"Growth exact boundary and fixed pending result");
            int pending=p.PendingSpecies;double hunger=p.Hunger;engine.Tick(1000);Check(p.Age==Engine.AdultAge&&p.Hunger==hunger&&p.PendingSpecies==pending,"Growth waiting freezes states and result");
            var decoded=Storage.Decode(Storage.Encode(data));Check(decoded.Pets[0].PendingSpecies==pending&&decoded.Pets[0].GrowthReady,"Pending growth survives serialization");
            engine.Reveal(p);Check(p.SpeciesId==pending&&!p.GrowthReady&&data.Discovered.Contains(pending),"Reveal registers discovery");
            p.Training=12;Check(!engine.Candidates(p).Any(s=>s.Id==6),"High training excludes lazy adult");
            Check(engine.Candidates(p).Count>0,"Candidates remain available");
            p.Training=0;p.TrainCooldown=0;int seeds=data.Seeds;engine.FinishActivity(p,true);Check(p.Training==1&&data.Seeds==seeds+8,"Completed training rewards once");
            engine.FinishActivity(p,true);Check(p.Training==1&&data.Seeds==seeds+8,"Repeated training cannot farm growth rewards");
            p.Waste=2;p.Dirt=70;engine.Clean(p);Check(p.Waste==1&&p.Dirt==50,"Cleaning is per waste");
            p.Age=Engine.Life-1;p.Illness=0;p.Hunger=0;p.Dirt=0;p.Fatigue=0;engine.Tick(5);Check(p.Dead&&p.Age==Engine.Life&&p.Cause=="자연사","Natural death fixed at 100 hours");
            data.Seeds=0;var rescue=engine.Adopt("새싹");Check(rescue!=null,"Rescue when all dead and no funds");
            Check(engine.Adopt("불가")==null,"Insufficient seeds cannot adopt extra pet");
            data.Seeds=100;engine.Adopt("둘째");Check(data.Seeds==0,"Paid adoption consumes seeds");
            data.Seeds=8;Check(engine.Buy(false)&&data.Seeds==0&&data.Dew==2,"Shop and shared inventory");
            // Basic meals every two hours plus direct cleaning fund an adoption by first growth.
            var economy=new SaveData();var basic=new Engine(economy,5);var baby=basic.Adopt("기본돌봄",true);
            for(int hour=0;hour<8;hour++){for(int minute=0;minute<60;minute++){basic.Tick(60);while(baby.Waste>0&&!baby.GrowthReady)basic.Clean(baby);}if(hour%2==1&&!baby.GrowthReady)basic.Feed(baby,false);}
            basic.Reveal(baby);Check(baby.SpeciesId>=0&&baby.Age==Engine.AdultAge,"Basic care reaches growth after three hours");
            var untouched=new SaveData();var untouchedEngine=new Engine(untouched,1);var untouchedPet=untouchedEngine.Adopt("기본후보",true);Check(untouchedEngine.Candidates(untouchedPet).Count==Catalog.Current.Count(),"Low interaction retains basic candidates");
            untouchedPet.Training=100;for(int i=0;i<1000;i++)if(untouchedEngine.ChooseSpecies(untouchedPet)==6)throw new Exception("Excluded species selected");Check(true,"Excluded species never selected in repeated draws");
            bool rejected=false;try{Storage.Decode("{\"Version\":4}");}catch{rejected=true;}Check(rejected,"Invalid saves rejected");
            var disease=new SaveData();var sickEngine=new Engine(disease);var sick=sickEngine.Adopt("아픈고구마",true);sick.Illness=99.99;sick.Hunger=100;sickEngine.Tick(60);Check(sick.Dead&&sick.Cause=="질병","Disease can kill before lifespan");
            var speech=new System.Collections.Generic.HashSet<string>();
            foreach(var species in Catalog.Current) {
                var speaker=new Pet {SpeciesId=species.Id,Hunger=6};var speechData=new SaveData();speechData.Pets.Add(speaker);var speechEngine=new Engine(speechData);
                string meal=speechEngine.Feed(speaker,false);
                Check(speaker.Hunger==0&&!meal.Contains("→")&&!meal.Contains("배고픔")&&speech.Add(meal),"Unique natural feeding dialogue: "+species.Name);
                Check(!String.IsNullOrEmpty(Dialogue.Activity(speaker,true))&&!String.IsNullOrEmpty(Dialogue.Activity(speaker,false))&&!String.IsNullOrEmpty(Dialogue.Clean(speaker))&&!String.IsNullOrEmpty(Dialogue.Greet(speaker)),"Activity dialogue: "+species.Name);
                speaker.Illness=30;Check(Dialogue.Need(speaker).Contains("치료"),"Illness remains visible in dialogue: "+species.Name);
            }
            Check(Dialogue.Options(new Pet {SpeciesId=30},0,"배가 든든하구마~").Contains(Dialogue.Get(new Pet {SpeciesId=30},0)),"Spy uses authored speech without numerical suffix");
            var revised=new SaveData {Seeds=90,StarterNutrientsClaimed=true};var revisedEngine=new Engine(revised,5);var young=revisedEngine.Adopt("경험치",true);
            revisedEngine.Tick(30);Check(young.Age==30&&young.GrowthExp==.5,"Fractional experience accumulates independently");
            young.Active=false;revisedEngine.Tick(120);Check(young.Age==30&&young.GrowthExp==.5,"Inactive freezes experience and age");young.Active=true;
            Check(revisedEngine.BuyNutrient()&&revised.Seeds==60&&revised.Nutrients==1&&young.GrowthExp==.5,"Nutrient purchase does not apply experience");
            Check(revisedEngine.Nourish(young)&&young.Age==30&&young.GrowthExp==30.5&&revised.Nutrients==0&&young.Affection==0,"Nutrient adds only experience and consumes inventory");
            revised.Nutrients=1;young.GrowthExp=175;Check(revisedEngine.Nourish(young)&&young.GrowthExp==180&&young.GrowthReady&&young.Age==30,"Nutrient overflow caps experience without aging");
            revised.Nutrients=1;Check(!revisedEngine.Nourish(young)&&revised.Nutrients==1,"Ready baby cannot consume nutrients");
            revisedEngine.Tick(60);Check(young.Age==30,"Growth waiting freezes age after accelerated growth");revisedEngine.Reveal(young);
            Check(!revisedEngine.Nourish(young)&&revised.Nutrients==1,"Adult cannot consume nutrients");revisedEngine.Tick(60);Check(young.Age==90&&young.GrowthExp==180,"Adult ages while experience stops");
            young.PetCooldown=0;young.Fatigue=99.5;double affection=young.Affection;int oldSeeds=revised.Seeds;revisedEngine.Stroke(young);
            Check(young.Fatigue==100&&young.Affection==affection+1&&young.PetCooldown==180&&revised.Seeds==oldSeeds+2,"Stroke increases fatigue and starts three minute cooldown");
            revisedEngine.Stroke(young);Check(young.Affection==affection+1&&revised.Seeds==oldSeeds+2,"Stroke during cooldown has no effects");
            young.Fatigue=0;revisedEngine.Tick(180);revisedEngine.Stroke(young);Check(young.Affection==affection+2,"Stroke becomes available after three minutes");
            young.TrainCooldown=180;oldSeeds=revised.Seeds;int oldGoals=young.Goals;Check(revisedEngine.ScoreGoal(young)&&revised.Seeds==oldSeeds+1&&young.Goals==oldGoals+1,"Goal pays immediately during cooldown");
            revisedEngine.FinishActivity(young,true);Check(revised.Seeds==oldSeeds+1&&young.Training==0,"Completion cannot pay a goal twice");
            young.TrainCooldown=0;revisedEngine.FinishActivity(young,true);Check(revised.Seeds==oldSeeds+9&&young.Training==1&&young.TrainCooldown==180,"Training completion pays eight seeds with three minute cooldown");
            young.PlayCooldown=0;revisedEngine.FinishActivity(young,false);Check(young.PlayCooldown==180,"Play cooldown is three minutes");
            young.Sleeping=true;oldSeeds=revised.Seeds;Check(!revisedEngine.ScoreGoal(young)&&revised.Seeds==oldSeeds,"Unavailable pet cannot score");young.Sleeping=false;
            var legacy=new SaveData {Version=1};legacy.Pets.Add(new Pet {Age=5400,PetCooldown=800,TrainCooldown=1200});legacy.Pets.Add(new Pet {Age=20000});legacy.Pets.Add(new Pet {Age=28800,GrowthReady=true,PendingSpecies=23});legacy.Pets.Add(new Pet {Age=40000,SpeciesId=30});
            var migrated=Storage.Decode(Storage.Encode(legacy));var migratedEngine=new Engine(migrated,4);
            Check(migrated.Version==3&&migrated.Pets[0].Age==5400&&migrated.Pets[0].GrowthExp==90&&migrated.Pets[0].PetCooldown==180,"Legacy baby progress migrates preserving age");
            Check(migrated.Pets[1].GrowthReady&&migrated.Pets[1].Age==20000&&migrated.Pets[1].PendingSpecies>=0,"Older legacy baby becomes ready without age reset");
            Check(migrated.Pets[2].PendingSpecies==23&&migrated.Pets[3].SpeciesId==30&&migrated.Pets[3].Age==40000,"Legacy growth result and adult retained");
            revised.Nutrients=2;var roundtrip=Storage.Decode(Storage.Encode(revised));Check(roundtrip.Nutrients==2&&roundtrip.Pets[0].GrowthExp==180,"New experience and inventory survive save roundtrip");
            bool invalidExp=false;young.GrowthExp=-1;try{Storage.Decode(Storage.Encode(revised));}catch{invalidExp=true;}Check(invalidExp,"Invalid experience rejected");
            var challenge=new TrainingRun();var challengeData=new SaveData();var challengePet=new Pet {SpeciesId=2};challengeData.Pets.Add(challengePet);var challengeEngine=new Engine(challengeData);
            for(int stage=0;stage<3;stage++){
                int limit=challenge.Limit;Check(limit==(stage==0?3:stage==1?5:7)&&challenge.Reward==stage+1,"Stage limit and reward");
                for(int shot=0;shot<limit;shot++){Check(challenge.BeginShot()&&!challenge.BeginShot(),"One shot in flight");challengeEngine.RecordShot(challengePet);Check(challenge.Score()&&!challenge.Score(),"Single goal per shot");challengeEngine.ScoreGoal(challengePet,challenge.Reward,challenge.TotalGoals);challenge.EndShot();challenge.EndShot();}
                if(stage==0)challengeEngine.FinishActivity(challengePet,true);
                Check(challenge.Complete&&!challenge.BeginShot(),"Stage cannot exceed attempt limit");Check(challenge.Advance()==(stage<2),"Perfect-only progression stops at final stage");
            }
            Check(challenge.TotalGoals==15&&challengeData.BestGoals==15&&challengeData.Seeds==42&&challengePet.Training==1&&challengePet.Shots==15,"Perfect run pays 34 goal seeds plus eight base seeds once");
            Check(Storage.Decode(Storage.Encode(challengeData)).BestGoals==15,"Best score survives restart");challengeEngine.ScoreGoal(challengePet,1,1);Check(challengeData.BestGoals==15,"Lower score cannot replace record");
            var missed=new TrainingRun();for(int shot=0;shot<3;shot++){missed.BeginShot();if(shot<2)missed.Score();missed.EndShot();}Check(!missed.Advance()&&missed.TotalGoals==2,"Miss prevents bonus entry");
            var rim=new System.Drawing.Rectangle(200,200,75,85);
            Check(BallPhysics.TouchesTop(new System.Drawing.PointF(185,180),new System.Drawing.PointF(185,220),rim),"Ball edge and overhanging rim count as goal");
            Check(BallPhysics.TouchesTop(new System.Drawing.PointF(220,300),new System.Drawing.PointF(220,100),rim),"Rising shot touching top counts");
            Check(!BallPhysics.TouchesTop(new System.Drawing.PointF(350,100),new System.Drawing.PointF(350,300),rim),"Far shot does not hit rim");
            Check(BallPhysics.HitsBody(new System.Drawing.PointF(0,230),new System.Drawing.PointF(400,230),rim)&&!BallPhysics.HitsBody(new System.Drawing.PointF(0,100),new System.Drawing.PointF(400,100),rim),"Swept body collision catches fast ball without distant false hit");
            double power=BallPhysics.Strength(1920,false)*BallPhysics.MaxDrag;Check(Math.Abs(power*power/BallPhysics.Gravity-1888)<1,"Full power reaches screen width");
            var edgePull=BallPhysics.Pull(new System.Drawing.PointF(100,900),new System.Drawing.PointF(16,964),1920,980);Check(Math.Abs(Math.Sqrt(edgePull.X*edgePull.X+edgePull.Y*edgePull.Y)-280)<.01,"Screen edge drag can reach full power without leaving monitor");
            foreach(var species in Catalog.Current){Check(!String.IsNullOrEmpty(Dialogue.Hit(new Pet {SpeciesId=species.Id})),"Species hit reaction");}
            Check(Dialogue.Options(new Pet {SpeciesId=2},8,"일부러 그런 거 아니지?")[0]=="일부러 그런 거 아니지?"&&Dialogue.Options(new Pet {SpeciesId=30},8,"감… 고구마 살려주구마!")[0]=="감… 고구마 살려주구마!","Requested hit reactions preserved");
            count+=DialogueTests.Run();
            count+=EvolutionTests.Run();count+=GrowthChoiceTests.Run();count+=BabySpriteTests.Run();count+=BerryCombatTests.Run();count+=CarrotSpriteTests.Run();
            count+=RunnerTests.Run();
            count+=LunchTests.Run();
            var hitTarget=new Pet();Check(ActivityWindow.CanHit(challengePet,hitTarget)&&!ActivityWindow.CanHit(challengePet,challengePet),"Other pet collision excludes thrower");hitTarget.Sleeping=true;Check(!ActivityWindow.CanHit(challengePet,hitTarget),"Sleeping pet excluded");
            return "PASS: "+count+" checks\r\nCare, growth, save migration, challenge progression, goal physics, rewards, best score and dialogue verified.\r\n";
        }
    }
    static class UiCheck {
        static void Check(bool condition,string label) {if(!condition)throw new Exception(label);}
        static ToolStripMenuItem Find(ContextMenuStrip menu,string prefix) {return menu.Items.OfType<ToolStripMenuItem>().First(i=>i.Text.StartsWith(prefix));}
        public static string Run() {
            Application.EnableVisualStyles();UiLayoutTests.Run();var data=new SaveData {Seeds=150,Dew=1,Medicine=1};var pet=new Pet {Name="테스트",Hunger=70};data.Pets.Add(pet);
            using(var app=new DesktopApp(data,null,false))try {
                Application.DoEvents();var w=app.Windows[pet.Id];Check(w.Visible&&w.TopMost&&w.TransparencyKey==System.Drawing.Color.Magenta,"Transparent topmost pet window");
                int initialX=w.Left,initialY=w.Top;w.Say("이동 중에도 말해요");w.Step(.5,true);Check(w.Left!=initialX,"Autonomous walking starts immediately and continues during speech");Check(w.Top!=initialY,"Autonomous walking also changes Y");
                var down=typeof(PetWindow).GetMethod("Down",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var up=typeof(PetWindow).GetMethod("Up",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                down.Invoke(w,new object[]{w,new MouseEventArgs(MouseButtons.Left,1,60,110,0)});Check(w.Capture,"Direct body press captures pointer without move menu");initialX=w.Left;w.Step(.5,true);Check(w.Left==initialX,"Holding body suspends autonomous walking");up.Invoke(w,new object[]{w,new MouseEventArgs(MouseButtons.Left,1,60,110,0)});Check(!w.Capture,"Releasing body releases pointer capture");
                var closingMenu=app.MenuFor(pet);closingMenu.Show(w,new System.Drawing.Point(30,90));Application.DoEvents();closingMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
                var outsideMenu=(DismissibleMenu)app.MenuFor(pet);outsideMenu.Show(w,new System.Drawing.Point(30,90));Application.DoEvents();outsideMenu.DismissOutside(new System.Drawing.Point(outsideMenu.Left+8,outsideMenu.Top+8));Check(outsideMenu.Visible,"Click inside menu preserves it");outsideMenu.DismissOutside(new System.Drawing.Point(outsideMenu.Left-30,outsideMenu.Top-30));Check(!outsideMenu.Visible&&!outsideMenu.IsDisposed,"Click outside closes menu without disposing item actions");closingMenu=outsideMenu;
                Check(!closingMenu.IsDisposed,"Menu survives Closed while item-click dispatch is pending");
                Find(closingMenu,"쓰다듬기").PerformClick();Check(pet.Affection==1,"Item action still runs after menu closes");
                var replacementMenu=app.MenuFor(pet);Check(closingMenu.IsDisposed&&!replacementMenu.IsDisposed,"Previous menu released on next opening");
                Check(!Find(replacementMenu,"쓰다듬기").Enabled&&Find(replacementMenu,"쓰다듬기").Text.Contains("00:03:00"),"Stroke menu disabled with remaining cooldown");
                data.Nutrients=1;double nutrientAge=pet.Age;using(var nutrientMenu=app.MenuFor(pet))Find(nutrientMenu,"성장 영양제").PerformClick();
                Check(pet.GrowthExp>=30&&pet.Age==nutrientAge&&data.Nutrients==0,"Nutrient menu consumes item without aging");
                using(var emptyMenu=app.MenuFor(pet))Check(!Find(emptyMenu,"성장 영양제").Enabled,"Nutrient menu disabled without inventory");
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
                w.Top=Screen.FromControl(w).WorkingArea.Top+180;pet.Y=w.Top;app.StartActivity(pet,true);Check(app.Activity!=null&&app.Activity.Visible,"Training overlay opens");
                var source=(System.Drawing.PointF)typeof(ActivityWindow).GetField("source",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(app.Activity);Check(Math.Abs(source.Y-(pet.Y-app.Activity.Top+PetWindow.BallAnchor.Y))<1,"Ball starts at relocated pet height");
                Check(app.Activity.Owner==w,"Activity owned by actor stays above actor");
                down.Invoke(w,new object[]{w,new MouseEventArgs(MouseButtons.Left,1,80,160,0)});app.Activity.EnsureForeground();
                IntPtr below=Native.GetWindow(app.Activity.Handle,2);bool actorBelow=false;for(int z=0;z<500&&below!=IntPtr.Zero;z++){if(below==w.Handle){actorBelow=true;break;}below=Native.GetWindow(below,2);}Check(actorBelow,"Character click leaves ball overlay above pet");
                app.Activity.SetPaused(true);app.Activity.SetPaused(false);var command=typeof(ActivityWindow).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);Check((bool)command.Invoke(app.Activity,new object[]{new Message(),Keys.Escape})&&app.Activity==null,"Escape command ends activity with control focus");Check(app.Activity==null,"Activity closes cleanly");
                pet.Age=Engine.AdultAge-1;pet.GrowthExp=Engine.AdultExp-1.0/60;app.Engine.Tick(1);w.Step(.125,false);int pending=pet.PendingSpecies;app.CompleteGrowth(pet,pending);Check(pet.SpeciesId==pending&&!pet.GrowthReady,"Growth reveals predetermined adult");
                Check((string)typeof(PetWindow).GetField("motion",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(w)=="stand","Growth reveal immediately resets burrow motion");

                var fileNames=new[]{"01.png","29.png"};Check(fileNames.All(file=>File.Exists(Path.Combine(Paths.BaseDirectory,"assets","higgsfield","characters",file))),"Packaged character assets");
                Check(Sprites.Home!=null,"Underground home background packaged");
                using(var bmp=new System.Drawing.Bitmap(120,120))using(var g=System.Drawing.Graphics.FromImage(bmp))foreach(string key in new[]{"stand","walk","eat","throw","burrow"}) {
                    g.Clear(System.Drawing.Color.Transparent);Check(Sprites.Draw(g,new System.Drawing.Rectangle(0,0,120,120),key,.625,false),"Sprite loaded: "+key);
                    bool visible=false;for(int y=0;y<120;y++)for(int x=0;x<120;x++){int alpha=bmp.GetPixel(x,y).A;if(alpha>0)visible=true;if(alpha!=0&&alpha!=255)throw new Exception("Sprite halo alpha was not removed");}Check(visible,"Sprite contains visible silhouette: "+key);
                }
                foreach(int id in Catalog.Current.Select(s=>s.Id))using(var first=new System.Drawing.Bitmap(120,120))using(var next=new System.Drawing.Bitmap(120,120))using(var a=System.Drawing.Graphics.FromImage(first))using(var b=System.Drawing.Graphics.FromImage(next)) {
                    Check(Art.ImageFor(id)!=null,"Adult asset: "+id);
                    foreach(string pose in new[]{"stand","walk","eat","throw","sleep","burrow"})for(int poseFrame=0;poseFrame<4;poseFrame++) {
                        b.Clear(System.Drawing.Color.Transparent);
                        Check(Sprites.Draw(b,new System.Drawing.Rectangle(0,0,120,120),pose,poseFrame*(pose=="sleep"?.5:.25),false,id),"Reviewed atlas loads: "+id+" / "+pose+" / "+poseFrame);
                        bool visible=false;for(int y=0;y<120;y++)for(int x=0;x<120;x++){int alpha=next.GetPixel(x,y).A;if(alpha>0)visible=true;Check(alpha==0||alpha==255,"Reviewed frame has no alpha halo");}
                        Check(visible,"Reviewed frame remains visible: "+id+" / "+pose+" / "+poseFrame);
                    }
                    foreach(string key in new[]{"walk","eat","throw","sleep","burrow"}) {
                        a.Clear(System.Drawing.Color.Transparent);b.Clear(System.Drawing.Color.Transparent);var p=new Pet {SpeciesId=id,Sleeping=key=="sleep",GrowthReady=key=="burrow"};
                        var box=new System.Drawing.Rectangle(0,0,120,120);Art.Pet(a,p,box,0,key);Art.Pet(b,p,box,.875,key);
                        bool changed=false;for(int y=0;y<120&&!changed;y++)for(int x=0;x<120;x++)if(first.GetPixel(x,y)!=next.GetPixel(x,y)){changed=true;break;}if(key=="burrow")Check(changed,"Adult burrow advances: "+id);
                    }
                }
                var spy=new Pet {SpeciesId=30,Name="간첩"};var spySave=new SaveData();spySave.Pets.Add(spy);spySave.Discovered.Add(30);Check(Storage.Decode(Storage.Encode(spySave)).Pets[0].SpeciesId==30,"Spy species save roundtrip");
                app.StartActivity(pet,false);var companion=new Pet {Name="간첩",SpeciesId=30,X=pet.X+220,Y=pet.Y};data.Pets.Add(companion);app.SyncWindows();var friendWindow=app.Windows[companion.Id];
                var hitMethod=typeof(ActivityWindow).GetMethod("HitOthers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var hitFrom=new System.Drawing.PointF(friendWindow.Left-app.Activity.Left-50,friendWindow.Top-app.Activity.Top+120);var hitTo=new System.Drawing.PointF(hitFrom.X+250,hitFrom.Y);
                hitMethod.Invoke(app.Activity,new object[]{hitFrom,hitTo});Check(Dialogue.Options(friendWindow.Pet,8,"감… 고구마 살려주구마!").Contains(friendWindow.Bubble),"Fetch collision displays spy reaction");
                string hitFeedback=(string)typeof(ActivityWindow).GetField("feedback",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(app.Activity);
                Check(hitFeedback==friendWindow.Pet.Name+": "+friendWindow.Bubble,"Fetch collision feedback matches bubble");friendWindow.Bubble="";
                hitMethod.Invoke(app.Activity,new object[]{hitFrom,hitTo});Check(friendWindow.Bubble=="","Repeated hit in same throw does not repeat interaction");app.Activity.CancelActivity();
                pet.TrainCooldown=0;app.StartActivity(pet,true);var challengeWindow=app.Activity;var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var challenge=(TrainingRun)typeof(ActivityWindow).GetField("run",flags).GetValue(challengeWindow);int challengeSeeds=data.Seeds,trainingBefore=pet.Training;
                var frame=typeof(ActivityWindow).GetMethod("Frame",flags);var reset=typeof(ActivityWindow).GetMethod("ResetBall",flags);
                for(int shot=0;shot<15;shot++){
                    typeof(ActivityWindow).GetField("celebration",flags).SetValue(challengeWindow,0.0);reset.Invoke(challengeWindow,null);challenge.BeginShot();challenge.Score();app.Engine.RecordShot(pet);app.Engine.ScoreGoal(pet,challenge.Reward,challenge.TotalGoals);
                    typeof(ActivityWindow).GetField("returning",flags).SetValue(challengeWindow,true);typeof(ActivityWindow).GetField("ball",flags).SetValue(challengeWindow,new System.Drawing.PointF(w.Left-challengeWindow.Left+PetWindow.BallAnchor.X,w.Top-challengeWindow.Top+PetWindow.BallAnchor.Y));frame.Invoke(challengeWindow,new object[]{null,EventArgs.Empty});
                }
                Check(challenge.TotalGoals==15&&data.Seeds==challengeSeeds+42&&pet.Training==trainingBefore+1&&data.BestGoals==15,"Actual activity advances 3-5-7 and grants base reward once");
                Check(challengeWindow.Visible&&(bool)typeof(ActivityWindow).GetField("finished",flags).GetValue(challengeWindow),"Finished record screen stays visible for capture");
                using(var capture=new System.Drawing.Bitmap(challengeWindow.Width,challengeWindow.Height)){challengeWindow.DrawToBitmap(capture,new System.Drawing.Rectangle(0,0,capture.Width,capture.Height));bool objectsGone=true;for(int y=140;y<capture.Height&&objectsGone;y++)for(int x=0;x<capture.Width;x++){int color=capture.GetPixel(x,y).ToArgb();if(color==System.Drawing.Color.Orange.ToArgb()||color==System.Drawing.Color.FromArgb(108,139,102).ToArgb()){objectsGone=false;break;}}Check(objectsGone,"Completed training hides both ball and goal while preserving record panel");capture.Save(Path.Combine(Paths.BaseDirectory,"training-preview.png"));}challengeWindow.CancelActivity();
                return "PASS: UI checks, including all 744 reviewed frame slots\r\nChallenge progression, rewards, completed object cleanup, record capture and pet collision verified.\r\n";
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
                using(var bmp=new System.Drawing.Bitmap(840,1920))using(var g=System.Drawing.Graphics.FromImage(bmp)) {
                    g.Clear(Art.Cream);string[] keys={"stand","walk","eat","throw","sleep","burrow"};
                    foreach(int id in Catalog.Current.Select(s=>s.Id))for(int k=0;k<keys.Length;k++){int x=(id%2)*420+k*70,y=(id/2)*120;Art.Pet(g,new Pet {SpeciesId=id,Sleeping=keys[k]=="sleep",GrowthReady=keys[k]=="burrow"},new System.Drawing.Rectangle(x,y+22,65,85),.875,keys[k]);g.DrawString(id.ToString("00")+" "+keys[k],System.Drawing.SystemFonts.DefaultFont,System.Drawing.Brushes.Black,x,y);}
                    bmp.Save(Path.Combine(Paths.BaseDirectory,"adult-actions-preview.png"));
                }
                home.Dispose();app.Exit(); // Render mode deliberately does not persist fixture data.
            }
        }
    }
}
