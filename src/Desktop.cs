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
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h,uint command);
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
    public class DismissibleMenu:ContextMenuStrip {
        Timer outsideClicks=new Timer {Interval=20};bool leftDown,rightDown;
        public DismissibleMenu(){AutoClose=true;outsideClicks.Tick+=(s,e)=>{
            short left=Native.GetAsyncKeyState(1),right=Native.GetAsyncKeyState(2);bool l=(left&0x8000)!=0,r=(right&0x8000)!=0;
            bool clicked=(l&&!leftDown)||(r&&!rightDown)||(left&1)!=0||(right&1)!=0;leftDown=l;rightDown=r;
            if(clicked)DismissOutside(Cursor.Position);
        };}
        public static bool ContainsMenu(ToolStripDropDown menu,Point point){if(menu.Visible&&menu.Bounds.Contains(point))return true;foreach(ToolStripItem item in menu.Items){var child=item as ToolStripDropDownItem;if(child!=null&&child.HasDropDownItems&&ContainsMenu(child.DropDown,point))return true;}return false;}
        public void DismissOutside(Point point){if(Visible&&!ContainsMenu(this,point))Close(ToolStripDropDownCloseReason.AppClicked);}
        protected override void OnOpened(EventArgs e){leftDown=(Native.GetAsyncKeyState(1)&0x8000)!=0;rightDown=(Native.GetAsyncKeyState(2)&0x8000)!=0;base.OnOpened(e);outsideClicks.Start();}
        protected override void OnClosed(ToolStripDropDownClosedEventArgs e){outsideClicks.Stop();base.OnClosed(e);}
        protected override void Dispose(bool disposing){if(disposing)outsideClicks.Dispose();base.Dispose(disposing);}
    }
    public static class Art {
        public static readonly Color Ink=Color.FromArgb(65,48,55), Cream=Color.FromArgb(255,246,221), Green=Color.FromArgb(91,126,74), Pink=Color.FromArgb(188,112,145);
        static Dictionary<int,Image> images=new Dictionary<int,Image>();
        public static Image ImageFor(int id) {
            if(id>=0){Image updated=Sprites.AdultStand(id);if(updated!=null)return updated;}
            if(id<0)return null;if(!images.ContainsKey(id)) {
                string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","characters",id.ToString("00")+".png");
                if(File.Exists(path)) {using(var source=Image.FromFile(path))images[id]=new Bitmap(source);}else images[id]=null;
            }return images[id];
        }
        public static void Pet(Graphics g,Pet p,Rectangle box,double phase,string motion="stand",bool flip=false) {
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            if(p.GrowthReady) {if(!Sprites.Draw(g,box,"burrow",phase,flip,p.SpeciesId,p.EvolutionId))Soil(g,box);return;}
            Image img=p.EvolutionId>=0?Sprites.EvolutionStand(p.EvolutionId):ImageFor(p.SpeciesId);
            if(p.Sleeping){if(!Sprites.Draw(g,box,"sleep",phase,flip,p.SpeciesId,p.EvolutionId)){if(img!=null)Adult(g,img,box,"sleep",phase,flip);else Baby(g,box);}}
            else if(img!=null){if(!Sprites.Draw(g,box,motion,phase,flip,p.SpeciesId,p.EvolutionId))Adult(g,img,box,motion,phase,flip);}else if(!Sprites.Draw(g,box,motion,phase,flip,p.SpeciesId,p.EvolutionId))Baby(g,box);
            if(p.Sleeping) {using(var f=new Font("Segoe UI",14,FontStyle.Bold))g.DrawString("z Z",f,Brushes.SlateBlue,box.Right-35,box.Top+8);}
            if(p.Illness>30)g.FillRectangle(Brushes.LightSkyBlue,box.Right-20,box.Top+30,6,10);
            if(p.Dirt>65) {using(var brush=new SolidBrush(Color.FromArgb(130,100,62,36))) {g.FillRectangle(brush,box.Left+box.Width/3,box.Top+box.Height*2/3,10,8);g.FillRectangle(brush,box.Left+box.Width/2,box.Top+box.Height/2,8,6);}}
            if(p.Age>=90*3600)g.DrawString("✧",SystemFonts.DefaultFont,Brushes.Gray,box.Left+8,box.Top+20);
        }
        public static void Adult(Graphics g,Image image,Rectangle box,string motion,double time,bool flip) {
            if(image==null){Soil(g,box);return;}
            double t=Math.Max(0,time),progress=Math.Min(1,t/1.875);float sx=1,sy=1,angle=0,lift=0;
            if(motion=="walk"){double step=Math.Sin(t*Math.PI*4);angle=(float)(step*7);sx=1+(float)Math.Abs(step)*.035f;sy=1-(float)Math.Abs(step)*.06f;lift=(float)Math.Abs(step)*box.Height*.07f;}
            else if(motion=="eat"){double sip=Math.Sin(t*Math.PI*6);sx=1+(float)sip*.04f;sy=1-(float)sip*.04f;angle=(float)(Math.Sin(t*Math.PI*2)*5);}
            else if(motion=="throw"){if(t<.625){angle=(float)(-20*t/.625);sx=1.06f;sy=.94f;}else {double recoil=Math.Max(0,1-(t-.625)/.6);angle=(float)(24*recoil);lift=(float)(box.Height*.08*recoil);}}
            else if(motion=="sleep"){sx=sy=1+(float)Math.Sin(t*2)*.008f;}
            else if(motion=="burrow"){sy=1-(float)progress*.2f;angle=(float)(Math.Sin(t*22)*8*(1-progress));}
            var state=g.Save();
            if(motion=="burrow")g.SetClip(new Rectangle(box.Left,box.Top,box.Width,Math.Max(1,box.Height-12)));
            g.TranslateTransform(box.Left+box.Width/2f,box.Bottom-lift+(motion=="burrow"?(float)progress*box.Height:0));
            g.ScaleTransform(flip?-sx:sx,sy);g.RotateTransform(angle);
            float scale=Math.Min(box.Width/(float)image.Width,box.Height/(float)image.Height);int drawWidth=Math.Max(1,(int)(image.Width*scale)),drawHeight=Math.Max(1,(int)(image.Height*scale));
            g.DrawImage(image,new Rectangle(-drawWidth/2,-drawHeight,drawWidth,drawHeight),0,0,image.Width,image.Height,GraphicsUnit.Pixel);g.Restore(state);
            if(motion=="eat"&&t<1.7){int x=flip?box.Left+box.Width/4:box.Left+box.Width*3/4,y=box.Top+box.Height*2/3;g.FillRectangle(Brushes.Sienna,x-8,y,18,17);g.FillRectangle(Brushes.LightSkyBlue,x-6,y,14,4);}
            if(motion=="burrow"){
                using(var earth=new SolidBrush(Color.FromArgb(126,87,54)))g.FillPolygon(earth,new[]{new Point(box.Left+4,box.Bottom-2),new Point(box.Left+box.Width/3,box.Bottom-25),new Point(box.Left+box.Width*2/3,box.Bottom-28),new Point(box.Right-4,box.Bottom-2)});
                if(progress>=1){int x=box.Left+box.Width/2;g.FillRectangle(Brushes.DarkOliveGreen,x,box.Bottom-40,3,18);g.FillRectangle(Brushes.OliveDrab,x-12,box.Bottom-42,14,6);g.FillRectangle(Brushes.YellowGreen,x+2,box.Bottom-47,13,6);}
            }
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
        public static void DisposeImages() {foreach(var i in images.Values)if(i!=null)i.Dispose();images.Clear();Sprites.Dispose();}
    }
    public class PetWindow:GameForm {
        public Pet Pet; public DesktopApp App; public string Bubble="";double bubbleLeft=0,phase=0;
        bool pressed=false,drag=false; Point offset,start;int direction=1;double wanderClock=6,walkX,walkY,headingX=.8,headingY=-.6;bool wandering=true;
        string motion="stand";double motionTime=0;bool walkingNow=false,growthSeen=false;
        public void Animate(string key,bool faceLeft=false) {if(motion!=key)motionTime=0;motion=key;direction=faceLeft?-1:1;Invalidate();}
        public static readonly Rectangle SpriteBounds=new Rectangle(18,52,84,84);
        public static readonly Point BallAnchor=new Point(62,115);
        public const int FloorOffset=136;
        public Rectangle ScaledSpriteBounds {get{return DisplayZoom.Rect(SpriteBounds,UiZoom);}}
        public Point ScaledBallAnchor {get{return new Point(DisplayZoom.Pixels(BallAnchor.X,UiZoom),DisplayZoom.Pixels(BallAnchor.Y,UiZoom));}}
        public int ScaledFloorOffset {get{return DisplayZoom.Pixels(FloorOffset,UiZoom);}}
        Rectangle Body {get {return ScaledSpriteBounds;}}
        public override void ApplyUiZoom(int percent){UiZoom=DisplayZoom.Normalize(percent)/100f;ClientSize=new Size(DisplayZoom.Pixels(126,UiZoom),DisplayZoom.Pixels(166,UiZoom));Location=Clamp(Location);Pet.X=Left;Pet.Y=Top;walkX=Left;walkY=Top;Invalidate();}
        public PetWindow(DesktopApp app,Pet pet) {
            App=app;Pet=pet;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;ClientSize=new Size(126,166);DoubleBuffered=true;Font=new Font("맑은 고딕",8);
            StartPosition=FormStartPosition.Manual;
            ApplyUiZoom(app.Engine.Data.DisplayScalePercent);
            if(pet.X==0 && pet.Y==0) {Rectangle area=Screen.PrimaryScreen.WorkingArea;pet.X=area.Left+70+(app.Windows.Count%7)*155;pet.Y=area.Bottom-Height;}
            Location=Clamp(new Point(pet.X,pet.Y));Pet.X=Left;Pet.Y=Top;
            walkX=Left;walkY=Top;MouseDown+=Down;MouseMove+=MoveMouse;MouseUp+=Up;MouseCaptureChanged+=(s,e)=>{if(!Capture){pressed=false;drag=false;walkX=Left;walkY=Top;}};
        }
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get {var cp=base.CreateParams;cp.ExStyle|=0x08000000|0x80;return cp;}}
        public void BeginMove() {Say("몸을 바로 드래그해서 옮길 수 있어요.");}
        public Point Clamp(Point point) {Rectangle area=Screen.FromPoint(new Point(point.X+Width/2,point.Y+Height/2)).WorkingArea;return new Point(Math.Max(area.Left,Math.Min(area.Right-Width,point.X)),Math.Max(area.Top,Math.Min(area.Bottom-Height,point.Y)));}
        void Down(object sender,MouseEventArgs e) {
            if(App.Activity!=null&&e.Button==MouseButtons.Left){App.Activity.EnsureForeground();return;}
            if(e.Button==MouseButtons.Right) {App.MenuFor(Pet).Show(this,e.Location);return;}
            if(e.Button!=MouseButtons.Left)return;
            if(App.Paused)return;
            if(e.Y>=DisplayZoom.Pixels(138,UiZoom) && Pet.Waste>0) {if(App.Engine.Clean(Pet)) {Say(Dialogue.Clean(Pet));App.Save();}return;}
            if(Pet.GrowthReady && Body.Contains(e.Location)) {App.Reveal(Pet);return;}
            if(Body.Contains(e.Location)&&App.Activity==null) {pressed=true;drag=false;Capture=true;offset=e.Location;start=Cursor.Position;}
        }
        void MoveMouse(object sender,MouseEventArgs e) {
            if(!pressed||(e.Button&MouseButtons.Left)==0||App.Paused)return;
            if(!drag && (Math.Abs(Cursor.Position.X-start.X)>=SystemInformation.DragSize.Width/2||Math.Abs(Cursor.Position.Y-start.Y)>=SystemInformation.DragSize.Height/2))drag=true;
            if(drag){Location=Clamp(new Point(Cursor.Position.X-offset.X,Cursor.Position.Y-offset.Y));Pet.X=Left;Pet.Y=Top;walkX=Left;walkY=Top;Invalidate();}
        }
        void Up(object sender,MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left||!pressed)return;bool moved=drag;pressed=false;drag=false;Capture=false;walkX=Left;walkY=Top;wanderClock=2;wandering=false;
            if(moved)App.Save();else if(!Pet.GrowthReady&&Body.Contains(e.Location))Say(Pet.Name+" · "+Pet.Kind+"\n배고픔 "+Math.Round(Pet.Hunger)+" / 병세 "+Math.Round(Pet.Illness));
        }
        public void Say(string text) {Bubble=text;bubbleLeft=8;Invalidate();}
        public void Step(double dt,bool walking) {
            phase+=dt;motionTime+=dt;bubbleLeft-=dt;if(bubbleLeft<=0)Bubble="";
            if(Pet.GrowthReady&&!growthSeen){Animate("burrow");growthSeen=true;}if(!Pet.GrowthReady){growthSeen=false;if(motion=="burrow")Animate("stand");}
            if((motion=="eat"||motion=="throw")&&motionTime>=2)motion="stand";
            walkingNow=false;
            if(walking && !pressed && !Pet.Sleeping && !Pet.GrowthReady && motion=="stand") {
                wanderClock-=dt;
                if(wanderClock<=0){wandering=!wandering;wanderClock=wandering?4+App.Engine.Random.NextDouble()*6:1+App.Engine.Random.NextDouble()*2;if(wandering){double angle=App.Engine.Random.NextDouble()*Math.PI*2;headingX=Math.Cos(angle);headingY=Math.Sin(angle);}}
                if(wandering){Rectangle area=Screen.FromControl(this).WorkingArea;
                    if(Left<=area.Left+8)headingX=Math.Abs(headingX);if(Right>=area.Right-8)headingX=-Math.Abs(headingX);
                    if(Top<=area.Top+8)headingY=Math.Abs(headingY);if(Bottom>=area.Bottom-8)headingY=-Math.Abs(headingY);
                    double speed=Pet.Skill=="민첩"?48:32;walkX+=headingX*speed*dt;walkY+=headingY*speed*dt;
                    walkX=Math.Max(area.Left,Math.Min(area.Right-Width,walkX));walkY=Math.Max(area.Top,Math.Min(area.Bottom-Height,walkY));
                    Location=new Point((int)Math.Round(walkX),(int)Math.Round(walkY));if(Math.Abs(headingX)>.05)direction=headingX<0?-1:1;Pet.X=Left;Pet.Y=Top;walkingNow=true;}
            }else {walkX=Left;walkY=Top;}
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var state=e.Graphics.Save();try{e.Graphics.ScaleTransform(UiZoom,UiZoom);PaintContent(e.Graphics);}finally{e.Graphics.Restore(state);}
        }
        void PaintContent(Graphics g){
            if(Bubble!="") {
                Rectangle r=new Rectangle(2,2,122,47);using(var b=new SolidBrush(Art.Cream))g.FillRectangle(b,r);using(var pen=new Pen(Art.Ink,1))g.DrawRectangle(pen,r);
                using(var bubbleFont=new Font("맑은 고딕",7))ZoomText.DrawText(g,Bubble,bubbleFont,new Rectangle(6,5,114,40),Art.Ink,TextFormatFlags.WordBreak|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
            Rectangle body=SpriteBounds;if(!Pet.Sleeping && !Pet.GrowthReady)body.Y+=(int)(Math.Sin(phase*3)*2);
            Art.Pet(g,Pet,body,Pet.GrowthReady?motionTime:motion=="stand"?phase:motionTime,walkingNow?"walk":motion,direction<0);
            if(Pet.Waste>0) {Art.Waste(g,new Rectangle(7,142,21,18));ZoomText.DrawText(g,Pet.Name+" · "+Pet.Waste+"개",Font,new Point(32,145),Art.Ink,Art.Cream);}
            else ZoomText.DrawText(g,Pet.Name,Font,new Rectangle(14,140,98,20),Art.Ink,Art.Cream,TextFormatFlags.HorizontalCenter);
        }
    }
    public class DesktopApp:ApplicationContext {
        public Engine Engine;public Dictionary<string,PetWindow> Windows=new Dictionary<string,PetWindow>();
        public HomeWindow Home; public ActivityWindow Activity;public DefenseWindow Defense;public RunnerWindow Runner;public LunchWindow Lunch;
        public UpdateWindow Update;
        public void OpenUpdate(){if(Update==null||Update.IsDisposed)Update=new UpdateWindow(this);Update.Show();Update.Activate();}
        public bool SaveForUpdate(){if(!persist)return true;try{Storage.Save(Engine.Data);return true;}catch(Exception ex){MessageBox.Show("업데이트 전에 저장하지 못했습니다.\n"+ex.Message,"저장 실패",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}}
        public bool ManualPause=false,AutoPause=false;bool suspended=false,locked=false,exiting=false,emergencyExiting=false;
        public bool Paused {get{return ManualPause||AutoPause||suspended||locked;}}
        NotifyIcon tray;ContextMenuStrip trayMenu;Icon icon,alertIcon;bool persist;Timer timer,emergencyTimer;Stopwatch watch=Stopwatch.StartNew();double last=0,saveClock=0,refreshClock=0,interactionClock=0;
        Dictionary<string,double> pairCooldown=new Dictionary<string,double>();HashSet<string> warnings=new HashSet<string>();
        Dictionary<string,ContextMenuStrip> petMenus=new Dictionary<string,ContextMenuStrip>();
        public DesktopApp(SaveData data,string warning,bool persistData=true) {
            persist=persistData;bool starterGift=!data.StarterNutrientsClaimed;Engine=new Engine(data);DisplayZoom.Percent=Engine.Data.DisplayScalePercent;if(starterGift)Save();icon=CreateIcon(false);alertIcon=CreateIcon(true);trayMenu=new ContextMenuStrip();tray=new NotifyIcon {Icon=icon,Text="GuMaGoChi · 고구마와 함께",Visible=true,ContextMenuStrip=trayMenu};
            tray.DoubleClick+=(s,e)=>OpenHome(null);tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)OpenHome(null);};
            RefreshTray();SystemEvents.PowerModeChanged+=Power;SystemEvents.SessionSwitch+=Session;
            timer=new Timer {Interval=125};timer.Tick+=Tick;timer.Start();
            emergencyTimer=new Timer {Interval=20};emergencyTimer.Tick+=(s,e)=>CheckEmergencyKeys((Native.GetAsyncKeyState((int)Keys.Space)&0x8000)!=0,(Native.GetAsyncKeyState((int)Keys.E)&0x8000)!=0);emergencyTimer.Start();
            if(data.Pets.Count==0) {OpenHome(null);Adopt(true);}else {SyncWindows();if(persist)OpenHome(null);}
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
                if(Defense==null||Defense.IsDisposed||!Defense.BattleRunning)Engine.Tick(dt);saveClock+=dt;refreshClock+=dt;interactionClock+=dt;
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
        string Talk(Pet p) { return Dialogue.Need(p)??(p.SpeciesId<0?Dialogue.Idle(p,Engine.Hint(p)):Dialogue.Get(p,3)); }
        void Interact() {
            if(Activity!=null || Windows.Values.Any(w=>w.Bubble!=""))return;
            var list=Windows.Values.Where(w=>w.Visible && Engine.CanCare(w.Pet)).ToArray();
            for(int i=0;i<list.Length;i++)for(int j=i+1;j<list.Length;j++) {
                var a=list[i];var b=list[j];string key=String.CompareOrdinal(a.Pet.Id,b.Pet.Id)<0?a.Pet.Id+"/"+b.Pet.Id:b.Pet.Id+"/"+a.Pet.Id;
                double due;if(pairCooldown.TryGetValue(key,out due)&&watch.Elapsed.TotalSeconds<due)continue;
                if(Math.Abs(a.Left-b.Left)>160 || Math.Abs(a.Top-b.Top)>90)continue;
                pairCooldown[key]=watch.Elapsed.TotalSeconds+600;
                int bond;a.Pet.Friends.TryGetValue(b.Pet.Id,out bond);a.Pet.Friends[b.Pet.Id]=bond+1;b.Pet.Friends[a.Pet.Id]=bond+1;
                a.Say(Dialogue.Greet(a.Pet));
                // Queue the second speech instead of overlapping ordinary bubbles.
                var response=new Timer {Interval=8500};response.Tick+=(s,e)=> {response.Stop();response.Dispose();if(!Paused && !b.IsDisposed && b.Visible && Engine.CanCare(b.Pet) && !Windows.Values.Any(w=>w.Bubble!=""))b.Say(Dialogue.Greet(b.Pet));};response.Start();Save();return;
            }
        }
        public void SyncWindows() {
            foreach(Pet p in Engine.Data.Pets) {
                bool visible=p.Active && !p.Dead && !p.Home && !Paused && (Defense==null||!Defense.BattleRunning);
                PetWindow w;if(!Windows.TryGetValue(p.Id,out w) && visible) {w=new PetWindow(this,p);Windows[p.Id]=w;w.Show();}
                if(w!=null){if(visible){if(!w.Visible)w.Show();}else w.Hide();}
            }
            foreach(var pair in Windows.ToArray())if(!Engine.Data.Pets.Contains(pair.Value.Pet)){pair.Value.Close();Windows.Remove(pair.Key);}
        }
        public void Say(Pet p,string text) {PetWindow w;if(Windows.TryGetValue(p.Id,out w)&&w.Visible)w.Say(text);else if(Home!=null&&!Home.IsDisposed)Home.ShowMessage(text);}
        public void Save() {if(!persist)return;try{Storage.Save(Engine.Data);warnings.Remove("save");}catch(Exception ex){if(emergencyExiting)return;if(!warnings.Contains("save")){warnings.Add("save");MessageBox.Show("저장에 실패했습니다.\n"+ex.Message,"저장 실패",MessageBoxButtons.OK,MessageBoxIcon.Error);}}}
        public void Change(Action change) {change();Save();SyncWindows();if(Home!=null&&!Home.IsDisposed)Home.RefreshData();RefreshTray();}
        public void Reveal(Pet p) {if(Paused)return;Change(()=>{if(Engine.Reveal(p)){PetWindow w;if(Windows.TryGetValue(p.Id,out w))w.Animate("stand");Say(p,p.Kind+" · "+p.Name+"\n"+Dialogue.Get(p,3));}});}
        public void OpenHome(Pet p) {if(Home==null||Home.IsDisposed)Home=new HomeWindow(this);Home.RefreshData();if(p!=null)Home.SelectPet(p.Id);Home.Show();Home.Activate();}
        public void OpenDefense(){if(Activity!=null||Runner!=null){OpenHome(null);Home.ShowMessage("놀이·훈련을 끝낸 뒤 디펜스를 시작해 주세요.");return;}if(Defense==null||Defense.IsDisposed)Defense=new DefenseWindow(this);Defense.Show();Defense.Activate();}
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
            var menu=new DismissibleMenu {Font=new Font("맑은 고딕",10),Padding=new Padding(4),BackColor=Art.Cream,ShowImageMargin=false,Renderer=new ReadableMenuRenderer()};petMenus[p.Id]=menu;
            menu.Items.Add(Item(p.Name+" · "+p.Kind,()=>OpenHome(p)));
            menu.Items.Add(new ToolStripLabel("배고픔 "+Math.Round(p.Hunger)+" · 청결 "+Math.Round(p.Dirt)+" · 피로 "+Math.Round(p.Fatigue)+" · 병세 "+Math.Round(p.Illness)));
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add(MenuHeading("돌봄"));bool care=!Paused&&Runner==null&&Engine.CanCare(p);
            var food=new ToolStripMenuItem("먹이 주기"+(p.FoodCooldown>0?" · "+Engine.TimeText(p.FoodCooldown):""));
            food.DropDownItems.Add(Item("수돗물 · 무료 · "+Math.Round(p.Hunger)+" → "+Math.Round(Math.Max(0,p.Hunger-10)),()=>Feed(p,false),care&&p.Hunger>0&&p.FoodCooldown<=0));
            food.DropDownItems.Add(Item("아침 이슬 · "+Engine.Data.Dew+"개 · "+Math.Round(p.Hunger)+" → "+Math.Round(Math.Max(0,p.Hunger-30)),()=>Feed(p,true),care&&p.Hunger>0&&p.FoodCooldown<=0&&Engine.Data.Dew>0));menu.Items.Add(food);
            var medicine=new ToolStripMenuItem("치료하기"+(p.MedicineCooldown>0?" · "+Engine.TimeText(p.MedicineCooldown):""));
            medicine.DropDownItems.Add(Item("기본 치료 · 무료 · 병세 −10",()=>Change(()=>Say(p,Engine.Treat(p,false))),care&&p.Illness>0&&p.MedicineCooldown<=0));
            medicine.DropDownItems.Add(Item("상위 치료 · "+Engine.Data.Medicine+"개 · 병세 −30",()=>Change(()=>Say(p,Engine.Treat(p,true))),care&&p.Illness>0&&p.MedicineCooldown<=0&&Engine.Data.Medicine>0));menu.Items.Add(medicine);
            menu.Items.Add(Item("쓰다듬기"+(p.PetCooldown>0?" · "+Engine.TimeText(p.PetCooldown):" · 피로 +1"),()=>Change(()=>Say(p,Engine.Stroke(p))),care&&p.PetCooldown<=0));
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add(MenuHeading("성장"));
            menu.Items.Add(Item("성장 영양제 먹이기 · "+Engine.Data.Nutrients+"개 · +30 EXP",()=>Nourish(p),care&&Engine.CanNourish(p)&&Engine.Data.Nutrients>0));
            if(Evolutions.For(p.SpeciesId).Any()) {
                bool changeable=care&&Activity==null&&(Defense==null||!Defense.BattleRunning);
                var evolutionMenu=new ToolStripMenuItem("2차 진화 모습 선택") {Enabled=changeable};
                foreach(var evolution in Evolutions.For(p.SpeciesId)) {
                    int target=evolution.Id;
                    var choice=Item(evolution.Name,()=>Change(()=>{if(Evolutions.Change(Engine.Data,p,target)){PetWindow window;if(Windows.TryGetValue(p.Id,out window))window.Animate("stand");Say(p,Evolutions.RevealLine(p));}}),p.EvolutionId!=target);
                    choice.Checked=p.EvolutionId==target;evolutionMenu.DropDownItems.Add(choice);
                }
                evolutionMenu.DropDownItems.Add(new ToolStripSeparator());
                evolutionMenu.DropDownItems.Add(Item("1차 모습으로 돌아가기",()=>Change(()=>Evolutions.Change(Engine.Data,p,-1)),p.EvolutionId>=0));
                menu.Items.Add(evolutionMenu);
            }
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add(MenuHeading("놀이 · 훈련"));
            menu.Items.Add(Item("공 가져오기"+(p.PlayCooldown>0?" · 성장 보상 대기 "+Engine.TimeText(p.PlayCooldown):""),()=>StartActivity(p,false),care&&!p.Home&&Activity==null));
            menu.Items.Add(Item("골인 훈련"+(p.TrainCooldown>0?" · 성장 보상 대기 "+Engine.TimeText(p.TrainCooldown):""),()=>StartActivity(p,true),care&&!p.Home&&Activity==null));
            menu.Items.Add(Item("고구마 밭 달리기 · 최고 "+Engine.Data.RunnerBest+"점",()=>StartRunner(p),care&&Activity==null&&Runner==null&&(Defense==null||!Defense.BattleRunning)));
            menu.Items.Add(Item("점심 뭐 먹지? · 곡선 사다리",()=>OpenLunch(p),care&&Activity==null&&Runner==null&&(Defense==null||!Defense.BattleRunning)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuHeading("생활 · 관리"));if(p.GrowthReady)menu.Items.Add(Item("흙더미 열기 · 성장하기",()=>Reveal(p),!Paused));
            menu.Items.Add(Item(p.Home?"외출하기":"집에 가기",()=>Change(()=>{p.Home=!p.Home;if(p.Home)OpenHome(p);}),!p.Dead&&!p.Sleeping&&Activity==null));
            menu.Items.Add(Item(p.Sleeping?"깨우기":"집에서 재우기",()=>Change(()=>{p.Home=true;p.Sleeping=!p.Sleeping;p.SleepClock=0;OpenHome(p);}),!p.Dead&&p.Active&&!p.GrowthReady&&Activity==null));
            menu.Items.Add(Item("위치 옮기기 · 몸을 잡아 드래그",()=>{PetWindow w;if(Windows.TryGetValue(p.Id,out w))w.BeginMove();},!p.Home&&p.Active&&!Paused&&Activity==null));
            menu.Items.Add(Item(p.Active?"비활성화 · 시간 멈추기":"활성화하기",()=>Change(()=>p.Active=!p.Active),!p.Dead&&Activity==null));
            menu.Items.Add(Item("이름 변경",()=>{using(var d=new NameDialog("이름 변경",p.Name))if(d.ShowDialog()==DialogResult.OK)Change(()=>p.Name=d.PetName);}));
            menu.Items.Add(Item("상태·가방·도감 열기",()=>OpenHome(p)));
            menu.Items.Add(ScaleMenu());
            menu.Items.Add(Item("비상탈출 · Space + E",EmergencyExit));
            menu.Items.Add(new ToolStripSeparator());
            var remove=Item("맛탕 만들기",()=>MakeMattang(p),Engine.Data.Pets.Contains(p)&&!p.Dead);remove.ForeColor=Color.Firebrick;menu.Items.Add(remove);StyleMenu(menu.Items);return menu;
        }
        static ToolStripLabel MenuHeading(string text){return new ToolStripLabel(text) {ForeColor=Art.Green,TextAlign=ContentAlignment.MiddleLeft};}
        static void StyleMenu(ToolStripItemCollection items){foreach(ToolStripItem item in items){if(!(item is ToolStripSeparator))item.Padding=new Padding(12,4,12,4);var parent=item as ToolStripMenuItem;if(parent!=null&&parent.HasDropDownItems){parent.DropDown.Font=parent.Owner.Font;parent.DropDown.BackColor=Art.Cream;parent.DropDown.Renderer=parent.Owner.Renderer;StyleMenu(parent.DropDownItems);}}}
        public void MakeMattang(Pet p) {
            if(!Engine.Data.Pets.Contains(p))return;
            string text="‘"+p.Name+"’로 맛탕을 만들까요?\n이 고구마와 돌봄 기록을 삭제하며, 추억 앨범에는 남지 않습니다.";
            if(MessageBox.Show(text,"맛탕 만들기",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            if(Activity!=null&&Activity.Pet==p)Activity.CancelActivity();if(Runner!=null)Runner.Close();
            Change(()=>{if(Engine.MakeMattang(p))warnings.Remove(p.Id);});
            if(Home!=null&&!Home.IsDisposed)Home.ShowMessage(p.Name+"로 맛탕을 만들었어요.");
        }
        public void Feed(Pet p,bool dew) {Change(()=>{double before=p.Hunger;Say(p,Engine.Feed(p,dew));if(p.Hunger<before){PetWindow w;if(Windows.TryGetValue(p.Id,out w))w.Animate("eat");if(Home!=null&&!Home.IsDisposed)Home.AnimateMeal(p);}});}
        public void Nourish(Pet p) {
            if(Paused||!Engine.CanNourish(p)||Engine.Data.Nutrients<=0)return;
            if(p.GrowthExp>150&&MessageBox.Show("남은 성장 경험치는 "+(Engine.AdultExp-p.GrowthExp).ToString("0.##")+" EXP입니다. 초과분은 적용되지 않습니다. 사용할까요?","성장 영양제",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            Change(()=>{if(Engine.Nourish(p)){Say(p,Dialogue.Get(p,0));PetWindow w;if(Windows.TryGetValue(p.Id,out w))w.Animate(p.GrowthReady?"burrow":"eat");if(Home!=null&&!Home.IsDisposed)Home.AnimateMeal(p);}});
        }
        public void StartActivity(Pet p,bool train) {if(Paused||Activity!=null||Runner!=null||(Defense!=null&&Defense.BattleRunning)||!Engine.CanCare(p)||p.Home)return;Activity=new ActivityWindow(this,p,train);Activity.Show();Activity.Activate();}
        public void StartRunner(Pet p){if(Paused||Activity!=null||Runner!=null||(Defense!=null&&Defense.BattleRunning)||!Engine.CanCare(p))return;Runner=new RunnerWindow(this,p);Runner.Show();Runner.Activate();}
        public void OpenLunch(Pet p){if(Lunch!=null&&!Lunch.IsDisposed){Lunch.Activate();return;}if(Paused||Activity!=null||Runner!=null||(Defense!=null&&Defense.BattleRunning)||!Engine.CanCare(p))return;Lunch=new LunchWindow(this,p);Lunch.Show();Lunch.Activate();}
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
            trayMenu.Items.Add(Item("업데이트 확인 · v"+Updates.CurrentText,OpenUpdate));
            trayMenu.Items.Add(ScaleMenu());
            trayMenu.Items.Add(Item("비상탈출 · Space + E",EmergencyExit));
            trayMenu.Items.Add(Item("종료 · 저장 후 닫기",Exit));tray.Icon=needs.Length>0?alertIcon:icon;tray.Text=needs.Length>0?"GuMaGoChi · "+needs.Length+"마리 돌봄 필요":"GuMaGoChi · 고구마와 함께";
        }
        void ToggleAutoStart() {try {using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {if(Engine.Data.AutoStart)key.DeleteValue("GuMaGoChi",false);else key.SetValue("GuMaGoChi","\""+Application.ExecutablePath+"\"");}Change(()=>Engine.Data.AutoStart=!Engine.Data.AutoStart);}catch(Exception ex){MessageBox.Show(ex.Message,"자동 실행 설정 실패");}}
        public void CheckEmergencyKeys(bool spaceDown,bool eDown){if(spaceDown&&eDown&&!exiting)EmergencyExit();}
        public void SetDisplayScale(int percent){
            if(Activity!=null||Runner!=null||(Defense!=null&&Defense.BattleRunning))return;
            Engine.Data.DisplayScalePercent=DisplayZoom.Normalize(percent);DisplayZoom.Percent=Engine.Data.DisplayScalePercent;
            foreach(var window in Application.OpenForms.Cast<Form>().OfType<GameForm>().ToArray())window.ApplyUiZoom(DisplayZoom.Percent);
            foreach(var window in Windows.Values)window.ApplyUiZoom(DisplayZoom.Percent);
            Save();RefreshTray();
        }
        public ToolStripMenuItem ScaleMenu(){
            var menu=new ToolStripMenuItem("캐릭터·UI 비율 · "+Engine.Data.DisplayScalePercent+"%") {Enabled=Activity==null&&Runner==null&&(Defense==null||!Defense.BattleRunning)};
            for(int percent=10;percent<=200;percent+=10){int value=percent;var option=Item(percent+"%"+(percent==100?" (기본)":""),()=>SetDisplayScale(value));option.Checked=percent==Engine.Data.DisplayScalePercent;menu.DropDownItems.Add(option);}
            return menu;
        }
        public void EmergencyExit(){
            if(exiting)return;
            emergencyExiting=true;
            ManualPause=true;
            // Hide every window before closing activities and writing the save.
            var openWindows=Application.OpenForms.Cast<Form>().ToArray();
            foreach(Form window in openWindows)window.Hide();
            Exit();
            foreach(Form window in openWindows)if(!window.IsDisposed)window.Dispose();
            if(persist)Environment.Exit(0);
        }
        public void Exit() {if(exiting)return;exiting=true;timer.Stop();if(Update!=null&&!Update.IsDisposed)Update.Close();if(emergencyTimer!=null){emergencyTimer.Stop();emergencyTimer.Dispose();}if(Lunch!=null&&!Lunch.IsDisposed)Lunch.Close();if(Runner!=null&&!Runner.IsDisposed)Runner.Close();if(Activity!=null)Activity.CancelActivity();if(Defense!=null&&!Defense.IsDisposed)Defense.Close();Save();SystemEvents.PowerModeChanged-=Power;SystemEvents.SessionSwitch-=Session;foreach(var w in Windows.Values)w.Close();if(Home!=null)Home.Dispose();foreach(var menu in petMenus.Values)if(!menu.IsDisposed)menu.Dispose();petMenus.Clear();tray.Visible=false;tray.Dispose();trayMenu.Dispose();icon.Dispose();alertIcon.Dispose();timer.Dispose();Art.DisposeImages();ExitThread();}
    }
    public class NameDialog:GameForm {
        TextBox input;public string PetName {get{return input.Text.Trim();}}
        public NameDialog(string title,string initial) {
            Text=title;ClientSize=new Size(410,210);AutoScaleMode=AutoScaleMode.Font;FormBorderStyle=FormBorderStyle.FixedDialog;StartPosition=FormStartPosition.CenterScreen;MaximizeBox=false;MinimizeBox=false;BackColor=Art.Cream;Font=new Font("맑은 고딕",10);
            Controls.Add(new Label {Text="함께할 고구마의 이름을 지어 주세요. (1~20자)",Left=20,Top=20,Width=370,Height=44});input=new TextBox {Text=initial,Left=20,Top=72,Width=370,MaxLength=20,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};Controls.Add(input);
            var ok=UiLayout.DialogButton("함께하기");ok.Click+=(s,e)=>{if(PetName.Length>0){DialogResult=DialogResult.OK;Close();}};
            var cancel=UiLayout.DialogButton("나중에");cancel.DialogResult=DialogResult.Cancel;Controls.Add(UiLayout.DialogActions(ok,cancel));AcceptButton=ok;CancelButton=cancel;AutoScaleDimensions=CurrentAutoScaleDimensions;
        }
    }
}
