using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GuMaGoChi {
    public static class Native {
        [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
        public static bool FullScreen() {
            IntPtr h=GetForegroundWindow();uint pid;GetWindowThreadProcessId(h,out pid);
            if(h==IntPtr.Zero || pid==(uint)Process.GetCurrentProcess().Id)return false;
            Rect r;if(!GetWindowRect(h,out r))return false;
            // Explorer desktop is deliberately excluded.
            try {string name=Process.GetProcessById((int)pid).ProcessName;if(name=="explorer")return false;}catch {return false;}
            Rectangle b=Screen.FromHandle(h).Bounds;
            return r.Left<=b.Left && r.Top<=b.Top && r.Right>=b.Right && r.Bottom>=b.Bottom;
        }
    }
    public static class Art {
        public static readonly Color Ink=Color.FromArgb(65,48,55), Cream=Color.FromArgb(255,246,221), Green=Color.FromArgb(91,126,74), Pink=Color.FromArgb(188,112,145);
        static Dictionary<int,Image> images=new Dictionary<int,Image>();
        public static Image ImageFor(int id) {
            if(id<0)return null;if(!images.ContainsKey(id)) {
                string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","characters",id.ToString("00")+".png");
                if(File.Exists(path)) {using(var source=Image.FromFile(path))images[id]=new Bitmap(source);}else images[id]=null;
            }return images[id];
        }
        public static void Pet(Graphics g,Pet p,Rectangle box,double phase) {
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            if(p.GrowthReady) {Soil(g,box);return;}
            Image img=ImageFor(p.SpeciesId);
            if(img!=null)g.DrawImage(img,box);else Baby(g,box);
            if(p.Sleeping) {using(var f=new Font("Segoe UI",14,FontStyle.Bold))g.DrawString("z Z",f,Brushes.SlateBlue,box.Right-35,box.Top+8);}
            if(p.Illness>30)g.FillRectangle(Brushes.LightSkyBlue,box.Right-20,box.Top+30,6,10);
            if(p.Dirt>65) {using(var brush=new SolidBrush(Color.FromArgb(130,100,62,36))) {g.FillRectangle(brush,box.Left+box.Width/3,box.Top+box.Height*2/3,10,8);g.FillRectangle(brush,box.Left+box.Width/2,box.Top+box.Height/2,8,6);}}
            if(p.Age>=90*3600)g.DrawString("✧",SystemFonts.DefaultFont,Brushes.Gray,box.Left+8,box.Top+20);
        }
        public static void Baby(Graphics g,Rectangle box) {
            // Temporary code-native pixel art; a commissioned baby sprite can replace this renderer.
            var state=g.Save();g.TranslateTransform(box.X,box.Y);g.ScaleTransform(box.Width/32f,box.Height/32f);
            using(var outline=new SolidBrush(Ink))using(var body=new SolidBrush(Color.FromArgb(180,131,161)))using(var earth=new SolidBrush(Color.FromArgb(120,83,51))) {
                g.FillRectangle(outline,10,9,12,3);g.FillRectangle(outline,7,12,18,14);g.FillRectangle(outline,10,26,12,3);
                g.FillRectangle(body,10,11,12,15);g.FillRectangle(body,8,14,16,9);g.FillRectangle(outline,5,20,3,4);g.FillRectangle(outline,24,20,3,4);
                g.FillRectangle(outline,10,27,3,3);g.FillRectangle(outline,20,27,3,3);
                g.FillRectangle(earth,10,12,4,2);g.FillRectangle(earth,18,23,5,3);g.FillRectangle(earth,9,22,3,3);
                g.FillRectangle(Brushes.DarkOliveGreen,15,6,2,5);g.FillRectangle(Brushes.OliveDrab,11,4,5,3);g.FillRectangle(Brushes.YellowGreen,17,3,5,3);
                g.FillRectangle(outline,11,17,2,3);g.FillRectangle(outline,20,17,2,3);g.FillRectangle(Brushes.White,11,17,1,1);g.FillRectangle(Brushes.White,20,17,1,1);g.FillRectangle(outline,16,21,2,1);
            }g.Restore(state);
        }
        public static void Soil(Graphics g,Rectangle box) {
            using(var brush=new SolidBrush(Color.FromArgb(126,87,54)))g.FillPolygon(brush,new[]{new Point(box.Left+8,box.Bottom-18),new Point(box.Left+25,box.Bottom-42),new Point(box.Left+55,box.Bottom-58),new Point(box.Right-22,box.Bottom-40),new Point(box.Right-5,box.Bottom-18)});
            g.FillRectangle(Brushes.DarkOliveGreen,box.Left+box.Width/2,box.Bottom-78,5,22);
            g.FillRectangle(Brushes.OliveDrab,box.Left+box.Width/2-16,box.Bottom-82,19,10);
            g.FillRectangle(Brushes.YellowGreen,box.Left+box.Width/2+4,box.Bottom-88,18,10);
        }
        public static void Waste(Graphics g,Rectangle r) {using(var b=new SolidBrush(Color.FromArgb(127,87,51))) {g.FillEllipse(b,r.X,r.Y+10,r.Width,r.Height-10);g.FillEllipse(b,r.X+5,r.Y+4,r.Width-10,r.Height-8);g.FillEllipse(b,r.X+10,r.Y,r.Width-20,12);} }
        public static void DisposeImages() {foreach(var i in images.Values)if(i!=null)i.Dispose();images.Clear();}
    }
    public class PetWindow:Form {
        public Pet Pet; public DesktopApp App; public string Bubble="";double bubbleLeft=0,phase=0;
        bool moving=false,drag=false; Point offset,start;double strokeTime=0;int moveSamples=0;int direction=1;
        Rectangle Body {get {return new Rectangle(26,74,120,120);}}
        public PetWindow(DesktopApp app,Pet pet) {
            App=app;Pet=pet;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;ClientSize=new Size(180,236);DoubleBuffered=true;Font=new Font("맑은 고딕",9);
            StartPosition=FormStartPosition.Manual;
            if(pet.X==0 && pet.Y==0) {Rectangle area=Screen.PrimaryScreen.WorkingArea;pet.X=area.Left+70+(app.Windows.Count%7)*155;pet.Y=area.Bottom-Height;}
            Location=Clamp(new Point(pet.X,pet.Y));Pet.X=Left;Pet.Y=Top;
            MouseDown+=Down;MouseMove+=MoveMouse;MouseUp+=Up;
        }
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get {var cp=base.CreateParams;cp.ExStyle|=0x08000000|0x80;return cp;}}
        public void BeginMove() {moving=true;Say("위치 이동: 몸을 잡고 원하는 곳에 놓아 주세요.");}
        public Point Clamp(Point point) {Rectangle area=Screen.FromPoint(new Point(point.X+Width/2,point.Y+Height/2)).WorkingArea;return new Point(Math.Max(area.Left,Math.Min(area.Right-Width,point.X)),Math.Max(area.Top,Math.Min(area.Bottom-Height,point.Y)));}
        void Down(object sender,MouseEventArgs e) {
            if(e.Button==MouseButtons.Right) {App.MenuFor(Pet).Show(this,e.Location);return;}
            if(e.Button!=MouseButtons.Left)return;
            if(App.Paused)return;
            if(e.Y>=198 && Pet.Waste>0) {if(App.Engine.Clean(Pet)) {Say("깨끗해졌어요!");App.Save();}return;}
            if(Pet.GrowthReady && Body.Contains(e.Location)) {App.Reveal(Pet);return;}
            if(Body.Contains(e.Location)) {drag=moving;Capture=true;offset=e.Location;start=e.Location;moveSamples=0;strokeTime=0;}
        }
        void MoveMouse(object sender,MouseEventArgs e) {
            if((e.Button&MouseButtons.Left)==0||App.Paused)return;
            if(drag) {Location=Clamp(new Point(Cursor.Position.X-offset.X,Cursor.Position.Y-offset.Y));Pet.X=Left;Pet.Y=Top;}
            else if(Body.Contains(e.Location) && Math.Abs(e.X-start.X)+Math.Abs(e.Y-start.Y)>4) {moveSamples++;start=e.Location;strokeTime+=.1;if(moveSamples>=6) {Say(App.Engine.Stroke(Pet));moveSamples=0;App.Save();}}
        }
        void Up(object sender,MouseEventArgs e) {Capture=false;if(drag){drag=false;moving=false;App.Save();}else if(moveSamples==0 && !Pet.GrowthReady && Body.Contains(e.Location) && strokeTime==0)Say(Pet.Name+" · "+Pet.Kind+"\n배고픔 "+Math.Round(Pet.Hunger)+" / 병세 "+Math.Round(Pet.Illness));}
        public void Say(string text) {Bubble=text;bubbleLeft=8;Invalidate();}
        public void Step(double dt,bool walking) {
            phase+=dt;bubbleLeft-=dt;if(bubbleLeft<=0)Bubble="";
            if(walking && !moving && !drag && !Pet.Sleeping && !Pet.GrowthReady && Bubble=="" && Math.Sin(phase/11)>.5) {
                Rectangle area=Screen.FromControl(this).WorkingArea;int speed=Pet.Skill=="민첩"?2:1;
                if(Left<=area.Left+8)direction=1;if(Right>=area.Right-8)direction=-1;
                Left+=direction*speed;Pet.X=Left;Pet.Y=Top;
            }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);Graphics g=e.Graphics;
            if(Bubble!="") {
                Rectangle r=new Rectangle(3,3,174,67);using(var b=new SolidBrush(Art.Cream))g.FillRectangle(b,r);using(var pen=new Pen(Art.Ink,2))g.DrawRectangle(pen,r);
                TextRenderer.DrawText(g,Bubble,Font,new Rectangle(9,8,162,56),Art.Ink,TextFormatFlags.WordBreak|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            Rectangle body=Body;if(!Pet.Sleeping && !Pet.GrowthReady)body.Y+=(int)(Math.Sin(phase*3)*2);
            Art.Pet(g,Pet,body,phase);
            if(Pet.Waste>0) {Art.Waste(g,new Rectangle(10,203,30,26));TextRenderer.DrawText(g,Pet.Name+" · "+Pet.Waste+"개",Font,new Point(46,207),Art.Ink,Art.Cream);}
            else TextRenderer.DrawText(g,Pet.Name,Font,new Rectangle(20,200,140,23),Art.Ink,Art.Cream,TextFormatFlags.HorizontalCenter);
        }
    }
    public class DesktopApp:ApplicationContext {
        public Engine Engine;public Dictionary<string,PetWindow> Windows=new Dictionary<string,PetWindow>();
        public HomeWindow Home; public ActivityWindow Activity;
        public bool ManualPause=false,AutoPause=false;bool suspended=false,locked=false,exiting=false;
        public bool Paused {get{return ManualPause||AutoPause||suspended||locked;}}
        NotifyIcon tray;ContextMenuStrip trayMenu;Icon icon,alertIcon;bool persist;Timer timer;Stopwatch watch=Stopwatch.StartNew();double last=0,saveClock=0,refreshClock=0,interactionClock=0;
        Dictionary<string,double> pairCooldown=new Dictionary<string,double>();HashSet<string> warnings=new HashSet<string>();
        Dictionary<string,ContextMenuStrip> petMenus=new Dictionary<string,ContextMenuStrip>();
        public DesktopApp(SaveData data,string warning,bool persistData=true) {
            persist=persistData;Engine=new Engine(data);icon=CreateIcon(false);alertIcon=CreateIcon(true);trayMenu=new ContextMenuStrip();tray=new NotifyIcon {Icon=icon,Text="GuMaGoChi · 고구마와 함께",Visible=true,ContextMenuStrip=trayMenu};
            tray.DoubleClick+=(s,e)=>OpenHome(null);tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)OpenHome(null);};
            RefreshTray();SystemEvents.PowerModeChanged+=Power;SystemEvents.SessionSwitch+=Session;
            timer=new Timer {Interval=250};timer.Tick+=Tick;timer.Start();
            if(data.Pets.Count==0) {OpenHome(null);Adopt(true);}else SyncWindows();
            if(warning!=null)MessageBox.Show(warning,"저장 복구");
        }
        Icon CreateIcon(bool alert) {using(var bmp=new Bitmap(32,32)) {using(Graphics g=Graphics.FromImage(bmp)){Art.Baby(g,new Rectangle(0,0,32,32));if(alert){g.FillEllipse(Brushes.OrangeRed,21,0,11,11);g.DrawEllipse(Pens.White,21,0,10,10);}}IntPtr h=bmp.GetHicon();try{return (Icon)Icon.FromHandle(h).Clone();}finally{Native.DestroyIcon(h);}}}
        void Power(object sender,PowerModeChangedEventArgs e) {if(e.Mode==PowerModes.Suspend)suspended=true;if(e.Mode==PowerModes.Resume)suspended=false;last=watch.Elapsed.TotalSeconds;Save();}
        void Session(object sender,SessionSwitchEventArgs e) {if(e.Reason==SessionSwitchReason.SessionLock)locked=true;if(e.Reason==SessionSwitchReason.SessionUnlock)locked=false;last=watch.Elapsed.TotalSeconds;Save();}
        void Tick(object sender,EventArgs e) {
            double now=watch.Elapsed.TotalSeconds,dt=Math.Min(2,Math.Max(0,now-last));last=now;
            bool was=Paused;AutoPause=Native.FullScreen();if(was!=Paused){SyncWindows();if(Activity!=null)Activity.SetPaused(Paused);}
            if(!Paused) {
                int deadBefore=Engine.Data.Pets.Count(p=>p.Dead),readyBefore=Engine.Data.Pets.Count(p=>p.GrowthReady);
                Engine.Tick(dt);saveClock+=dt;refreshClock+=dt;interactionClock+=dt;
                if(deadBefore!=Engine.Data.Pets.Count(p=>p.Dead)) {Save();tray.ShowBalloonTip(8000,"추억이 남았어요","고구마가 떠났어요. 함께한 기록은 추억 앨범에서 볼 수 있어요.",ToolTipIcon.Info);}
                if(readyBefore!=Engine.Data.Pets.Count(p=>p.GrowthReady))Save();
                foreach(var pair in Windows.ToArray()) {
                    PetWindow w=pair.Value;Pet p=w.Pet;
                    if(p.Dead || p.Home || !p.Active)continue;
                    w.Step(dt,Activity==null || Activity.Pet!=p);
                    if(p.GrowthReady && w.Bubble=="")w.Say("성장할 준비가 됐어요!\n흙더미를 눌러 주세요.");
                    if(!p.GrowthReady && !p.Sleeping && p.TalkClock<=0 && !Windows.Values.Any(v=>v.Bubble!="")) {w.Say(Talk(p));p.TalkClock=Engine.Random.Next(900,1801);}
                }
                if(interactionClock>=5) {Interact();interactionClock=0;}
                foreach(Pet p in Engine.Data.Pets.Where(p=>p.Active && !p.Dead && p.Illness>=70))if(warnings.Add(p.Id))tray.ShowBalloonTip(8000,"치료가 필요해요",p.Name+"의 병세가 위중해요. 집을 열어 확인해 주세요.",ToolTipIcon.Warning);
                warnings.RemoveWhere(id=>Engine.Data.Pets.Any(p=>p.Id==id && (p.Dead||p.Illness<60)));
                if(saveClock>=30){Save();saveClock=0;}
                if(refreshClock>=1){SyncWindows();if(Home!=null&&!Home.IsDisposed)Home.RefreshData();RefreshTray();refreshClock=0;}
            }
        }
        string Talk(Pet p) {
            if(p.Hunger>60)return "배가 슬슬 고파요…";if(p.Illness>20)return "몸이 조금 무거워요.";if(p.Fatigue>75)return "땅속 잠자리가 생각나요.";
            if(p.Age>=90*3600)return "너와 함께한 시간이 참 따뜻했어.";
            if(p.SpeciesId<0)return Engine.Hint(p);
            string[] lines={"오늘은 어떤 일이 생길까?","같이 있으니까 심심하지 않네.","흙 냄새가 참 좋아."};
            switch(p.SpeciesId) {case 10:return "방금 소리… 들었느냐? 무서운 건 아니다.";case 12:return p.Waste>0?"갑판 청결 상태가 엉망이군.":"깨끗한 공으로 준비하게.";case 23:return "쓰다듬어도 된다고는 안 했는데…";case 27:return "내가 봤을 때는 잠깐 쉬어도 되겠어.";case 24:return "누가 공을 움직였어?! …그냥 물어본 거야.";case 28:return "한 판만 더. 이번엔 연습 아니야.";case 3:return "오늘도 수고했어. 천천히 해도 괜찮아.";case 9:return "다음 공은 꼭 넣고 말겠어!";}
            return p.Affection>=10 && Engine.Random.Next(3)==0?"네가 오는 발소리는 이제 알아.":lines[Engine.Random.Next(lines.Length)];
        }
        void Interact() {
            if(Activity!=null || Windows.Values.Any(w=>w.Bubble!=""))return;
            var list=Windows.Values.Where(w=>w.Visible && Engine.CanCare(w.Pet)).ToArray();
            for(int i=0;i<list.Length;i++)for(int j=i+1;j<list.Length;j++) {
                var a=list[i];var b=list[j];string key=String.CompareOrdinal(a.Pet.Id,b.Pet.Id)<0?a.Pet.Id+"/"+b.Pet.Id:b.Pet.Id+"/"+a.Pet.Id;
                double due;if(pairCooldown.TryGetValue(key,out due)&&watch.Elapsed.TotalSeconds<due)continue;
                if(Math.Abs(a.Left-b.Left)>160 || Math.Abs(a.Top-b.Top)>90)continue;
                pairCooldown[key]=watch.Elapsed.TotalSeconds+600;
                int bond;a.Pet.Friends.TryGetValue(b.Pet.Id,out bond);a.Pet.Friends[b.Pet.Id]=bond+1;b.Pet.Friends[a.Pet.Id]=bond+1;
                a.Say(a.Pet.SpeciesId==10?"내 뒤에 있으면 안전하다!":bond>=3?b.Pet.Name+", 옆에 앉아도 돼?":b.Pet.Name+", 안녕!");
                // Queue the second speech instead of overlapping ordinary bubbles.
                var response=new Timer {Interval=8500};response.Tick+=(s,e)=> {response.Stop();response.Dispose();if(!Paused && !b.IsDisposed && b.Visible && Engine.CanCare(b.Pet) && !Windows.Values.Any(w=>w.Bubble!=""))b.Say(b.Pet.SpeciesId==27?"네가 제일 먼저 숨었잖아.":bond>=3?"응, 여기 같이 쉬자.":"반가워. 같이 걸을래?");};response.Start();Save();return;
            }
        }
        public void SyncWindows() {
            foreach(Pet p in Engine.Data.Pets) {
                bool visible=p.Active && !p.Dead && !p.Home && !Paused;
                PetWindow w;if(!Windows.TryGetValue(p.Id,out w) && visible) {w=new PetWindow(this,p);Windows[p.Id]=w;w.Show();}
                if(w!=null){if(visible){if(!w.Visible)w.Show();}else w.Hide();}
            }
            foreach(var pair in Windows.ToArray())if(!Engine.Data.Pets.Contains(pair.Value.Pet)){pair.Value.Close();Windows.Remove(pair.Key);}
        }
        public void Say(Pet p,string text) {PetWindow w;if(Windows.TryGetValue(p.Id,out w)&&w.Visible)w.Say(text);else if(Home!=null&&!Home.IsDisposed)Home.ShowMessage(text);}
        public void Save() {if(!persist)return;try{Storage.Save(Engine.Data);warnings.Remove("save");}catch(Exception ex){if(!warnings.Contains("save")){warnings.Add("save");MessageBox.Show("저장에 실패했습니다.\n"+ex.Message,"저장 실패",MessageBoxButtons.OK,MessageBoxIcon.Error);}}}
        public void Change(Action change) {change();Save();SyncWindows();if(Home!=null&&!Home.IsDisposed)Home.RefreshData();RefreshTray();}
        public void Reveal(Pet p) {if(Paused)return;Change(()=>{if(Engine.Reveal(p))Say(p,"짜잔! "+p.Kind+"로 자랐어요!\n이름은 여전히 "+p.Name+"예요.");});}
        public void OpenHome(Pet p) {if(Home==null||Home.IsDisposed)Home=new HomeWindow(this);Home.RefreshData();if(p!=null)Home.SelectPet(p.Id);Home.Show();Home.Activate();}
        public void Adopt(bool first=false) {
            if(!first && Engine.Data.Pets.Any(p=>!p.Dead) && Engine.Data.Seeds<Engine.AdoptPrice){MessageBox.Show("입양에는 씨앗 100개가 필요해요.");return;}
            using(var dialog=new NameDialog("새 아기 고구마의 이름", ""))if(dialog.ShowDialog()==DialogResult.OK)Change(()=>{Pet p=Engine.Adopt(dialog.PetName,first);if(p!=null){SyncWindows();Say(p,"안녕! 나는 "+p.Name+"야.\n잘 부탁해!");}});
        }
        ToolStripMenuItem Item(string title,Action action,bool enabled=true) {var i=new ToolStripMenuItem(title);i.Enabled=enabled;i.Click+=(s,e)=>action();return i;}
        public ContextMenuStrip MenuFor(Pet p) {
            // WinForms continues dispatching item clicks after Closed. Keep the
            // menu alive until the next opening instead of disposing inside Closed.
            ContextMenuStrip previousMenu;
            if(petMenus.TryGetValue(p.Id,out previousMenu)) {
                if(previousMenu.Visible)return previousMenu;
                if(!previousMenu.IsDisposed)previousMenu.Dispose();
            }
            var menu=new ContextMenuStrip {Font=new Font("맑은 고딕",9)};petMenus[p.Id]=menu;
            menu.Items.Add(Item(p.Name+" · "+p.Kind,()=>OpenHome(p)));
            menu.Items.Add(new ToolStripLabel("배고픔 "+Math.Round(p.Hunger)+" · 청결 "+Math.Round(p.Dirt)+" · 피로 "+Math.Round(p.Fatigue)+" · 병세 "+Math.Round(p.Illness)));
            menu.Items.Add(new ToolStripSeparator());bool care=!Paused&&Engine.CanCare(p);
            var food=new ToolStripMenuItem("먹이 주기"+(p.FoodCooldown>0?" · "+Engine.TimeText(p.FoodCooldown):""));
            food.DropDownItems.Add(Item("수돗물 · 무료 · "+Math.Round(p.Hunger)+" → "+Math.Round(Math.Max(0,p.Hunger-10)),()=>Change(()=>Say(p,Engine.Feed(p,false))),care&&p.Hunger>0&&p.FoodCooldown<=0));
            food.DropDownItems.Add(Item("아침 이슬 · "+Engine.Data.Dew+"개 · "+Math.Round(p.Hunger)+" → "+Math.Round(Math.Max(0,p.Hunger-30)),()=>Change(()=>Say(p,Engine.Feed(p,true))),care&&p.Hunger>0&&p.FoodCooldown<=0&&Engine.Data.Dew>0));menu.Items.Add(food);
            var medicine=new ToolStripMenuItem("치료하기"+(p.MedicineCooldown>0?" · "+Engine.TimeText(p.MedicineCooldown):""));
            medicine.DropDownItems.Add(Item("기본 치료 · 무료 · 병세 −10",()=>Change(()=>Say(p,Engine.Treat(p,false))),care&&p.Illness>0&&p.MedicineCooldown<=0));
            medicine.DropDownItems.Add(Item("상위 치료 · "+Engine.Data.Medicine+"개 · 병세 −30",()=>Change(()=>Say(p,Engine.Treat(p,true))),care&&p.Illness>0&&p.MedicineCooldown<=0&&Engine.Data.Medicine>0));menu.Items.Add(medicine);
            menu.Items.Add(Item("쓰다듬기",()=>Change(()=>Say(p,Engine.Stroke(p))),care));
            menu.Items.Add(Item("공 가져오기"+(p.PlayCooldown>0?" · 성장 보상 대기 "+Engine.TimeText(p.PlayCooldown):""),()=>StartActivity(p,false),care&&!p.Home&&Activity==null));
            menu.Items.Add(Item("골인 훈련"+(p.TrainCooldown>0?" · 성장 보상 대기 "+Engine.TimeText(p.TrainCooldown):""),()=>StartActivity(p,true),care&&!p.Home&&Activity==null));
            menu.Items.Add(new ToolStripSeparator());
            if(p.GrowthReady)menu.Items.Add(Item("흙더미 열기 · 성장하기",()=>Reveal(p),!Paused));
            menu.Items.Add(Item(p.Home?"외출하기":"집에 가기",()=>Change(()=>{p.Home=!p.Home;if(p.Home)OpenHome(p);}),!p.Dead&&!p.Sleeping&&Activity==null));
            menu.Items.Add(Item(p.Sleeping?"깨우기":"집에서 재우기",()=>Change(()=>{p.Home=true;p.Sleeping=!p.Sleeping;p.SleepClock=0;OpenHome(p);}),!p.Dead&&p.Active&&!p.GrowthReady&&Activity==null));
            menu.Items.Add(Item("위치 옮기기 · 몸을 잡아 드래그",()=>{PetWindow w;if(Windows.TryGetValue(p.Id,out w))w.BeginMove();},!p.Home&&p.Active&&!Paused&&Activity==null));
            menu.Items.Add(Item(p.Active?"비활성화 · 시간 멈추기":"활성화하기",()=>Change(()=>p.Active=!p.Active),!p.Dead&&Activity==null));
            menu.Items.Add(Item("이름 변경",()=>{using(var d=new NameDialog("이름 변경",p.Name))if(d.ShowDialog()==DialogResult.OK)Change(()=>p.Name=d.PetName);}));
            menu.Items.Add(Item("상태·가방·도감 열기",()=>OpenHome(p)));return menu;
        }
        public void StartActivity(Pet p,bool train) {if(Paused||Activity!=null||!Engine.CanCare(p)||p.Home)return;Activity=new ActivityWindow(this,p,train);Activity.Show();}
        void RefreshTray() {
            if(trayMenu.Visible)return;
            foreach(ToolStripItem old in trayMenu.Items.Cast<ToolStripItem>().ToArray())old.Dispose();
            trayMenu.Items.Clear();trayMenu.Items.Add(Item("땅속 집 · 개체 관리",()=>OpenHome(null)));trayMenu.Items.Add(Item("새 아기 입양 · 씨앗 "+Engine.Data.Seeds,()=>Adopt()));
            var needs=Engine.Data.Pets.Where(p=>p.Active&&!p.Dead&&!p.GrowthReady&&(p.Hunger>=50||p.Dirt>=45||p.Fatigue>=70||p.Illness>0)).ToArray();
            if(needs.Length>0){trayMenu.Items.Add(new ToolStripSeparator());foreach(Pet p in needs) {Pet target=p;trayMenu.Items.Add(Item("돌봄 필요: "+p.Name+" · "+(p.Illness>0?"치료":p.Hunger>=50?"식사":p.Dirt>=45?"청소":"수면"),()=>OpenHome(target)));}}
            trayMenu.Items.Add(new ToolStripSeparator());trayMenu.Items.Add(Item(ManualPause?"다시 함께하기":"잠시 쉬기 · 모두 멈춤",()=>{ManualPause=!ManualPause;SyncWindows();if(Activity!=null)Activity.SetPaused(Paused);RefreshTray();}));
            trayMenu.Items.Add(new ToolStripLabel(AutoPause?"전체 화면 작업 중 · 자동 일시정지":""));
            trayMenu.Items.Add(Item("윈도우 시작 시 자동 실행 "+(Engine.Data.AutoStart?"✓":""),ToggleAutoStart));
            trayMenu.Items.Add(Item("저장 폴더 열기",()=>{Directory.CreateDirectory(Storage.Folder);Process.Start("explorer.exe",Storage.Folder);}));
            trayMenu.Items.Add(Item("종료 · 저장 후 닫기",Exit));tray.Icon=needs.Length>0?alertIcon:icon;tray.Text=needs.Length>0?"GuMaGoChi · "+needs.Length+"마리 돌봄 필요":"GuMaGoChi · 고구마와 함께";
        }
        void ToggleAutoStart() {try {using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {if(Engine.Data.AutoStart)key.DeleteValue("GuMaGoChi",false);else key.SetValue("GuMaGoChi","\""+Application.ExecutablePath+"\"");}Change(()=>Engine.Data.AutoStart=!Engine.Data.AutoStart);}catch(Exception ex){MessageBox.Show(ex.Message,"자동 실행 설정 실패");}}
        public void Exit() {if(exiting)return;exiting=true;timer.Stop();if(Activity!=null)Activity.CancelActivity();Save();SystemEvents.PowerModeChanged-=Power;SystemEvents.SessionSwitch-=Session;foreach(var w in Windows.Values)w.Close();if(Home!=null)Home.Dispose();foreach(var menu in petMenus.Values)if(!menu.IsDisposed)menu.Dispose();petMenus.Clear();tray.Visible=false;tray.Dispose();trayMenu.Dispose();icon.Dispose();alertIcon.Dispose();timer.Dispose();Art.DisposeImages();ExitThread();}
    }
    public class NameDialog:Form {
        TextBox input;public string PetName {get{return input.Text.Trim();}}
        public NameDialog(string title,string initial) {
            Text=title;ClientSize=new Size(370,160);FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterScreen;MaximizeBox=false;MinimizeBox=false;BackColor=Art.Cream;Font=new Font("맑은 고딕",10);
            Controls.Add(new Label {Text="함께할 고구마의 이름을 지어 주세요. (1~20자)",Left=18,Top=20,Width=335,Height=25});input=new TextBox {Text=initial,Left=20,Top=60,Width=330,MaxLength=20};Controls.Add(input);
            var ok=new Button {Text="함께하기",Left=155,Top=108,Width=95};ok.Click+=(s,e)=>{if(PetName.Length>0){DialogResult=DialogResult.OK;Close();}};Controls.Add(ok);
            var cancel=new Button {Text="나중에",Left=260,Top=108,Width=90,DialogResult=DialogResult.Cancel};Controls.Add(cancel);AcceptButton=ok;CancelButton=cancel;
        }
    }
}
