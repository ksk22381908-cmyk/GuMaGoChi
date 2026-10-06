using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GuMaGoChi {
    public class DefenseWindow:GameForm {
        DesktopApp app;Panel sidebar,hud,cardPanel;ListBox available;DefenseSlot[] slots=new DefenseSlot[5];Button start,quit;Button[] cardButtons=new Button[3];Label cardTitle;
        DefenseCanvas field;Timer timer=new Timer {Interval=25};Stopwatch watch=Stopwatch.StartNew();double last;bool desktopMode,mouseWasDown,introActive,disposed;
        Rectangle setupBounds;Point listDown;int listIndex=-1;DefenseIntro intro;DefenseInfo info;Font small=new Font("맑은 고딕",8.5f);
        public DefenseBattle Battle;public DefenseArt Assets=new DefenseArt();public DefenseRoster Roster;
        public bool BattleRunning {get{return Battle!=null&&Battle.Running;}}
        public override void ApplyUiZoom(int percent){base.ApplyUiZoom(percent);LayoutPanels();}
        internal bool IntroActive {get{return introActive;}}
        internal Panel UpgradePanel {get{return cardPanel;}}
        internal Panel PartyPanel {get{return sidebar;}}
        public DefenseWindow(DesktopApp owner){
            app=owner;Text="고구마 버러지 디펜스 · 편성";ClientSize=new Size(1000,620);MinimumSize=new Size(820,560);BackColor=Art.Cream;Font=new Font("맑은 고딕",9);AutoScaleMode=AutoScaleMode.None;
            Roster=new DefenseRoster(app.Engine.Data.Pets);field=new DefenseCanvas(this) {Dock=DockStyle.Fill};Controls.Add(field);
            hud=new DefenseHud(this) {Width=360,Height=100,BackColor=Color.LemonChiffon};Controls.Add(hud);
            quit=new Button {Left=242,Top=7,Width=110,Height=28,MinimumSize=new Size(110,28),AutoSize=true,AutoSizeMode=AutoSizeMode.GrowOnly,Padding=new Padding(8,2,8,2),Text="이번 판 종료"};quit.Click+=(s,e)=>{if(BattleRunning){Battle.Finish();ShowResult();}else Close();};hud.Controls.Add(quit);
            sidebar=new Panel {Width=182,BackColor=Color.LemonChiffon};Controls.Add(sidebar);
            available=new ListBox {Left=8,Top=36,Width=166,Height=100,Font=small};sidebar.Controls.Add(available);RefreshAvailable();
            available.MouseDown+=(s,e)=>{listDown=e.Location;listIndex=available.IndexFromPoint(e.Location);};
            available.MouseMove+=(s,e)=>{if(e.Button==MouseButtons.Left&&listIndex>=0&&!BattleRunning&&(Math.Abs(e.X-listDown.X)+Math.Abs(e.Y-listDown.Y)>5)){var choice=available.Items[listIndex] as Choice;listIndex=-1;if(choice!=null)available.DoDragDrop(choice.Pet.Id,DragDropEffects.Move);}};
            available.DoubleClick+=(s,e)=>{var choice=available.SelectedItem as Choice;if(choice!=null&&!BattleRunning){int empty=Array.FindIndex(Roster.Slots,p=>p==null);if(empty>=0)AssignSlot(empty,choice.Pet.Id);}};
            for(int i=0;i<5;i++){slots[i]=new DefenseSlot(this,i) {Left=8,Width=166,Height=60};sidebar.Controls.Add(slots[i]);}
            start=new Button {Left=8,Width=166,Height=28,Text="전투 시작"};start.Click+=(s,e)=>StartBattle();sidebar.Controls.Add(start);
            sidebar.Paint+=(s,e)=>e.Graphics.DrawString(BattleRunning?"고구마 편성 · 클릭: 스킬":"고구마를 칸으로 드래그",small,Brushes.SaddleBrown,8,8);
            cardPanel=new Panel {Width=548,Height=186,BackColor=Color.LemonChiffon,Visible=false};Controls.Add(cardPanel);
            cardTitle=new Label {Left=12,Top=8,Width=520,Height=27,Text="강화 카드 1장을 선택하세요."};cardPanel.Controls.Add(cardTitle);
            for(int i=0;i<3;i++){int index=i;cardButtons[i]=new Button {Left=12+i*178,Top=39,Width=168,Height=133,TextAlign=ContentAlignment.MiddleCenter};cardButtons[i].Click+=(s,e)=>{if(Battle!=null&&Battle.ChooseCard(index)){cardPanel.Hide();field.Focus();}};cardPanel.Controls.Add(cardButtons[i]);}
            field.MouseDown+=FieldDown;Resize+=(s,e)=>LayoutPanels();
            timer.Tick+=(s,e)=>TickBattle();timer.Start();PreviewTeam();LayoutPanels();
            FormClosed+=(s,e)=>{if(Battle!=null)Battle.Finish();if(app.Defense==this)app.Defense=null;app.SyncWindows();app.Save();};
        }
        class Choice {public Pet Pet;public override string ToString(){return Pet.Name+" · "+Pet.Kind;}}
        void RefreshAvailable(){available.Items.Clear();foreach(var p in app.Engine.Data.Pets.Where(p=>!p.Dead))available.Items.Add(new Choice {Pet=p});}
        internal bool AssignSlot(int slot,string id){if(BattleRunning)return false;Pet pet=app.Engine.Data.Pets.FirstOrDefault(p=>p.Id==id&&!p.Dead);if(!Roster.Assign(slot,pet))return false;PreviewTeam();return true;}
        internal void ClearSlot(int slot){if(!BattleRunning&&Roster.Clear(slot))PreviewTeam();}
        void PreviewTeam(){var pets=Roster.Pets.Where(p=>app.Engine.Data.Pets.Contains(p)).ToArray();Battle=pets.Length==0?null:new DefenseBattle(app.Engine.Data,pets);if(Battle!=null){Roster.ApplySlots(Battle);Battle.Running=false;Battle.Notice="1~5번 칸에 편성하고 전투를 시작하세요.";}LayoutPanels();InvalidateAll();}
        internal void StartBattle(bool showIntro=true){if(app.Paused){MessageBox.Show("트레이에서 잠시 쉬기를 해제해 주세요.");return;}var pets=Roster.Pets.Where(p=>app.Engine.Data.Pets.Contains(p)).ToArray();if(pets.Length==0){MessageBox.Show("고구마를 한 마리 이상 편성해 주세요.");return;}Battle=new DefenseBattle(app.Engine.Data,pets);Roster.ApplySlots(Battle);Battle.Persist=app.Save;EnterDesktop(showIntro);field.Focus();last=watch.Elapsed.TotalSeconds;}
        void ShowResult(){if(info!=null&&!info.IsDisposed)info.Close();LeaveDesktop();RefreshAvailable();LayoutPanels();app.Save();if(app.Home!=null&&!app.Home.IsDisposed)app.Home.RefreshData();InvalidateAll();}
        void TickBattle(){double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.1,now-last);last=now;
            short left=Native.GetAsyncKeyState(1);bool down=(left&0x8000)!=0,clicked=down&&!mouseWasDown||!down&&(left&1)!=0;
            if(desktopMode&&clicked&&(Native.GetAsyncKeyState(16)&0x8000)!=0&&BattleRunning&&!introActive&&!Battle.ChoosingCard&&!app.Paused){Point cursor=field.PointToClient(Cursor.Position);if(field.ClientRectangle.Contains(cursor)&&!hud.Bounds.Contains(cursor))Battle.ToggleMarker(Logical(cursor));}
            mouseWasDown=down;if(BattleRunning&&!introActive&&!app.Paused)Battle.Step(dt);
            if(desktopMode&&Battle!=null&&!Battle.Running){ShowResult();return;}RefreshCards();InvalidateAll();
        }
        void InvalidateAll(){if(disposed)return;field.Invalidate();hud.Invalidate();sidebar.Invalidate();foreach(var slot in slots)if(slot!=null)slot.Invalidate();}
        PointF Logical(Point p){return new PointF(Math.Max(0,Math.Min(1000,p.X*1000f/field.Width)),Math.Max(100,Math.Min(540,p.Y*560f/field.Height)));}
        void FieldDown(object sender,MouseEventArgs e){if((ModifierKeys&Keys.Shift)!=0)return;field.Focus();if(Battle==null||introActive)return;PointF p=Logical(e.Location);var d=Battle.Team.FirstOrDefault(f=>Math.Abs(f.X-p.X)<30*UiZoom/(field.Width/1000f)&&Math.Abs(f.Y-p.Y)<35*UiZoom/(field.Height/560f));if(d==null)return;if(e.Button==MouseButtons.Right)ShowInfo(d.Slot);else if(e.Button==MouseButtons.Left&&BattleRunning&&!app.Paused)Battle.Skill(d);}
        internal void SlotClick(int index,MouseButtons button){if((ModifierKeys&Keys.Shift)!=0||introActive)return;if(button==MouseButtons.Right){ShowInfo(index);return;}var d=DefenderAt(index);if(button==MouseButtons.Left&&BattleRunning&&!app.Paused&&d!=null)Battle.Skill(d);}
        internal Defender DefenderAt(int slot){return Battle==null?null:Battle.Team.FirstOrDefault(d=>d.Slot==slot);}
        internal void ShowInfo(int slot){var d=DefenderAt(slot);if(d==null)return;if(info!=null&&!info.IsDisposed)info.Close();info=new DefenseInfo(d);Rectangle area=Screen.FromControl(this).WorkingArea;info.StartPosition=FormStartPosition.Manual;info.Location=new Point(Math.Max(area.Left,Math.Min(area.Right-info.Width,Cursor.Position.X-info.Width-12)),Math.Max(area.Top,Math.Min(area.Bottom-info.Height,Cursor.Position.Y-30)));info.Show(this);}
        internal static Rectangle RosterBounds(int width,int height,bool battle,float zoom=1){return new Rectangle(Math.Max(0,width-DisplayZoom.Pixels(194,zoom)),Math.Max(0,height-DisplayZoom.Pixels((battle?352:508)+12,zoom)),DisplayZoom.Pixels(182,zoom),DisplayZoom.Pixels(battle?352:508,zoom));}
        internal static Rectangle SlotBounds(int width,int height,bool battle,int slot,float zoom=1){Rectangle side=RosterBounds(width,height,battle,zoom);return new Rectangle(side.Left+DisplayZoom.Pixels(8,zoom),side.Top+DisplayZoom.Pixels((battle?28:156)+slot*62,zoom),DisplayZoom.Pixels(166,zoom),DisplayZoom.Pixels(60,zoom));}
        internal void ArrangeTeam(int width,int height){if(Battle==null)return;Battle.HitScaleX=width/1000f/UiZoom;Battle.HitScaleY=height/560f/UiZoom;foreach(var d in Battle.Team){Rectangle r=SlotBounds(width,height,BattleRunning,d.Slot,UiZoom);d.X=(r.Left+DisplayZoom.Pixels(49,UiZoom))*1000f/width;d.Y=(r.Top+DisplayZoom.Pixels(29,UiZoom))*560f/height;}}
        void LayoutPanels(){if(field==null||sidebar==null)return;hud.Location=new Point(DisplayZoom.Pixels(8,UiZoom),DisplayZoom.Pixels(8,UiZoom));sidebar.Bounds=RosterBounds(ClientSize.Width,ClientSize.Height,BattleRunning,UiZoom);available.Visible=!BattleRunning;start.Visible=!BattleRunning;start.Top=sidebar.Height-DisplayZoom.Pixels(36,UiZoom);for(int i=0;i<5;i++)slots[i].Top=DisplayZoom.Pixels((BattleRunning?28:156)+i*62,UiZoom);quit.Text=BattleRunning?"이번 판 종료":"닫기";quit.Left=hud.Width-quit.Width-DisplayZoom.Pixels(8,UiZoom);cardPanel.Location=new Point(Math.Max(8,(ClientSize.Width-182-cardPanel.Width)/2),Math.Max(115,(ClientSize.Height-cardPanel.Height)/2));ArrangeTeam(field.Width,field.Height);hud.BringToFront();sidebar.BringToFront();cardPanel.BringToFront();}
        internal void RefreshCards(){bool choosing=BattleRunning&&Battle.ChoosingCard&&!introActive;if(!choosing){cardPanel.Visible=false;return;}cardTitle.Text=Battle.Completed+"라운드 완료 · 카드 1장을 선택하세요.";for(int i=0;i<3;i++){var card=Battle.Cards[i];cardButtons[i].Text=card.Title+"\n\n"+card.Scope+"\n"+card.Description;}cardPanel.Visible=true;cardPanel.BringToFront();}
        internal void EnterDesktop(bool showIntro=true){if(!desktopMode)setupBounds=Bounds;desktopMode=true;MinimumSize=Size.Empty;FormBorderStyle=FormBorderStyle.None;TopMost=true;Bounds=Screen.FromControl(this).WorkingArea;BackColor=TransparencyKey=Color.Magenta;LayoutPanels();app.SyncWindows();if(app.Home!=null)app.Home.Hide();introActive=showIntro;mouseWasDown=(Native.GetAsyncKeyState(1)&0x8000)!=0;if(showIntro){intro=new DefenseIntro(Assets.Intro,Screen.FromControl(this).Bounds,()=>{introActive=false;Native.GetAsyncKeyState(1);mouseWasDown=true;last=watch.Elapsed.TotalSeconds;});intro.Show(this);}}
        internal void LeaveDesktop(){if(!desktopMode)return;if(intro!=null&&!intro.IsDisposed)intro.Skip();desktopMode=false;introActive=false;TopMost=false;TransparencyKey=Color.Empty;BackColor=Art.Cream;FormBorderStyle=FormBorderStyle.Sizable;Bounds=setupBounds;MinimumSize=new Size(820,560);cardPanel.Hide();LayoutPanels();app.SyncWindows();}
        internal void DrawHud(Graphics g,Rectangle r){var state=g.Save();g.TranslateTransform(r.Left,r.Top);g.ScaleTransform(UiZoom,UiZoom);DrawHudLogical(g,new Rectangle(0,0,(int)(r.Width/UiZoom),(int)(r.Height/UiZoom)));g.Restore(state);}
        void DrawHudLogical(Graphics g,Rectangle r){g.FillRectangle(Brushes.LemonChiffon,r);float x=r.Left+8,y=r.Top+6;g.DrawString("최고 "+app.Engine.Data.DefenseBestWave+"R / "+app.Engine.Data.DefenseBestKills+"처치",small,Brushes.SaddleBrown,x,y);if(Battle==null)return;g.DrawString("R"+Battle.Wave+" · "+Battle.Kills+"처치 · 씨앗 +"+Battle.Rewards,small,Brushes.SaddleBrown,x,y+20);g.FillRectangle(Brushes.Maroon,x,y+43,155,8);g.FillRectangle(Brushes.ForestGreen,x,y+43,(float)(155*Battle.House/100),8);g.DrawString("방어선 "+(int)Battle.House+" · 보호막 "+(int)Battle.Shield,small,Brushes.SaddleBrown,x+164,y+39);g.DrawString("Shift+클릭: 표식 켜기/끄기 · 우클릭: 능력치",small,Brushes.SaddleBrown,x,y+68);}
        internal void DrawSlot(Graphics g,Rectangle r,int slot){var state=g.Save();g.TranslateTransform(r.Left,r.Top);g.ScaleTransform(UiZoom,UiZoom);DrawSlotLogical(g,new Rectangle(0,0,(int)(r.Width/UiZoom),(int)(r.Height/UiZoom)),slot);g.Restore(state);}
        void DrawSlotLogical(Graphics g,Rectangle r,int slot){g.FillRectangle(Brushes.OldLace,r);g.DrawRectangle(Pens.Tan,r.Left,r.Top,r.Width-1,r.Height-1);g.DrawString((slot+1).ToString(),small,Brushes.SaddleBrown,r.Left+5,r.Top+21);var d=DefenderAt(slot);Pet pet=d==null?Roster.Slots[slot]:d.Pet;if(pet==null){g.DrawString("비어 있음",small,Brushes.DimGray,r.Left+44,r.Top+22);return;}Art.Pet(g,pet,new Rectangle(r.Left+25,r.Top+2,48,54),d==null?10:d.Motion,d!=null&&d.Motion<1.2?"throw":"stand",true);string name=pet.Name.Length>7?pet.Name.Substring(0,7)+"…":pet.Name;g.DrawString(name,small,Brushes.SaddleBrown,r.Left+80,r.Top+9);g.DrawString(BattleRunning&&d!=null?(d.SkillClock<=0?"스킬 준비":Math.Ceiling(d.SkillClock)+"초"):pet.Kind,small,BattleRunning&&d!=null&&d.SkillClock<=0?Brushes.DarkGreen:Brushes.DimGray,r.Left+80,r.Top+31);if(d!=null&&(d.PowerTime>0||d.SpeedTime>0||d.DoubleTime>0||d.SelfTime>0||d.EmpowerTime>0))g.DrawRectangle(Pens.Goldenrod,r.Left+23,r.Top+1,52,56);}
        void DrawRoster(Graphics g,int width,int height){var r=RosterBounds(width,height,BattleRunning,UiZoom);g.FillRectangle(Brushes.LemonChiffon,r);g.DrawString(BattleRunning?"고구마 편성 · 클릭: 스킬":"고구마를 칸으로 드래그",small,Brushes.SaddleBrown,r.Left+8,r.Top+8);for(int i=0;i<5;i++)DrawSlot(g,SlotBounds(width,height,BattleRunning,i,UiZoom),i);}
        float scaleX=1,scaleY=1;
        void At(Graphics g,PointF p,Action<Graphics> draw){var state=g.Save();g.TranslateTransform(p.X,p.Y);g.ScaleTransform(.7f*UiZoom/scaleX,.7f*UiZoom/scaleY);draw(g);g.Restore(state);}
        public void Render(Graphics g,int width,int height,bool unused=true){scaleX=width/1000f;scaleY=height/560f;ArrangeTeam(width,height);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.Clear(desktopMode?Color.Magenta:Art.Cream);if(Battle!=null){var world=g.Save();g.ScaleTransform(scaleX,scaleY);var battle=Battle;
                if(battle.Marker.HasValue)At(g,battle.Marker.Value,c=>{using(var pen=new Pen(Color.OrangeRed,3)){c.DrawEllipse(pen,-18,-10,36,20);c.DrawLine(pen,-25,0,25,0);c.DrawLine(pen,0,-18,0,18);}});
                foreach(var fx in battle.Effects)if(fx.Ground)DrawEffect(g,fx);
                foreach(var e in battle.Enemies){int size=e.Type==5?112:e.Type==4?40:75;At(g,e.Point,c=>{Assets.Draw(c,"enemies",e.Type,e.Frame,new PointF(),size);c.FillRectangle(Brushes.DarkRed,-25,-size/2-5,50,4);c.FillRectangle(Brushes.YellowGreen,-25,-size/2-5,(float)(50*Math.Max(0,e.HP/e.MaxHP)),4);});}
                foreach(var shot in battle.Shots)At(g,shot.Point,c=>{if(shot.Owner.Pet.SpeciesId<0)c.FillRectangle(Brushes.Sienna,-5,-5,10,10);else Assets.Draw(c,shot.Skill?"skills":"basic-attacks",shot.Owner.Pet.SpeciesId,0,new PointF(),shot.Skill?65:42);});
                foreach(var fx in battle.Effects)if(!fx.Ground)DrawEffect(g,fx);g.Restore(world);
            }
            DrawHud(g,new Rectangle(8,8,360,100));DrawRoster(g,width,height);
            if(Battle!=null){Rectangle side=RosterBounds(width,height,BattleRunning,UiZoom);foreach(var d in Battle.Team)if(d.BubbleClock>0){string text=d.BubbleLine;SizeF size=g.MeasureString(text,small);float x=Math.Max(8,side.Left-size.Width-20),y=d.Y*scaleY-10;g.FillRectangle(Brushes.LemonChiffon,x-5,y-4,size.Width+10,size.Height+8);g.DrawRectangle(Pens.SaddleBrown,x-5,y-4,size.Width+10,size.Height+8);g.DrawString(text,small,Brushes.SaddleBrown,x,y);}if(!BattleRunning){string message=Battle.Notice;g.DrawString(message,Font,Brushes.SaddleBrown,24,130);}}
        }
        void DrawEffect(Graphics g,DefenseEffect fx){if(fx.Species<0){At(g,fx.Point,c=>{using(var pen=new Pen(Color.Sienna,3))c.DrawEllipse(pen,-35,-12,70,24);});return;}int frame=fx.Skill?(fx.Life>.5?(fx.Age<.15?0:fx.Age<.3?1:fx.Life-fx.Age<.15?3:2):Math.Min(3,1+(int)(fx.Age/.1))):Math.Min(3,1+(int)(fx.Age/.1));At(g,fx.Point,c=>Assets.Draw(c,fx.Skill?"skills":"basic-attacks",fx.Species,frame,new PointF(0,fx.Skill&&fx.Ground?-(364f/384f-.5f)*135:0),fx.Skill?135:70));}
        protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Stop();timer.Dispose();if(Battle!=null)Battle.Finish();if(intro!=null&&!intro.IsDisposed)intro.Dispose();if(info!=null&&!info.IsDisposed)info.Dispose();Assets.Dispose();small.Dispose();}base.Dispose(disposing);}
    }
    public class DefenseSlot:Panel {
        DefenseWindow owner;int slot;Point pressed;bool dragging;Button remove;
        public DefenseSlot(DefenseWindow window,int index){owner=window;slot=index;DoubleBuffered=true;AllowDrop=true;remove=new Button {Text="×",Width=18,Height=20,Left=145,Top=1};remove.Click+=(s,e)=>owner.ClearSlot(slot);Controls.Add(remove);
            DragEnter+=(s,e)=>e.Effect=!owner.BattleRunning&&e.Data.GetDataPresent(DataFormats.Text)?DragDropEffects.Move:DragDropEffects.None;
            DragDrop+=(s,e)=>{string id=e.Data.GetData(DataFormats.Text) as string;if(id!=null)owner.AssignSlot(slot,id);};
            MouseDown+=(s,e)=>{pressed=e.Location;dragging=false;};MouseMove+=(s,e)=>{if(e.Button==MouseButtons.Left&&!owner.BattleRunning&&!dragging&&(Math.Abs(e.X-pressed.X)+Math.Abs(e.Y-pressed.Y)>5)){Pet p=owner.Roster.Slots[slot];if(p!=null){dragging=true;DoDragDrop(p.Id,DragDropEffects.Move);}}};MouseUp+=(s,e)=>{if(!dragging)owner.SlotClick(slot,e.Button);};
        }
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);remove.Visible=!owner.BattleRunning&&owner.Roster.Slots[slot]!=null;owner.DrawSlot(e.Graphics,ClientRectangle,slot);}
    }
    public class DefenseHud:Panel {DefenseWindow owner;public DefenseHud(DefenseWindow window){owner=window;DoubleBuffered=true;}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);owner.DrawHud(e.Graphics,ClientRectangle);}}
    public class DefenseCanvas:Panel {DefenseWindow owner;public DefenseCanvas(DefenseWindow window){owner=window;DoubleBuffered=true;TabStop=true;}protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);owner.Render(e.Graphics,Width,Height);}}
    public class DefenseIntro:GameForm {
        Timer timer=new Timer {Interval=25};Stopwatch age=Stopwatch.StartNew();Bitmap image;Action finished;bool completed;
        public DefenseIntro(Bitmap source,Rectangle bounds,Action finish=null){finished=finish;image=source==null?null:new Bitmap(source);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;Bounds=bounds;BackColor=Color.Black;DoubleBuffered=true;MouseDown+=(s,e)=>Skip();timer.Tick+=(s,e)=>{double seconds=age.Elapsed.TotalSeconds;if(seconds>=3.7){Skip();return;}Opacity=FadeOpacity(seconds);};timer.Start();}
        public void Skip(){Complete();Close();}
        void Complete(){if(completed)return;completed=true;timer.Stop();if(finished!=null)finished();}
        public static double FadeOpacity(double seconds){return seconds<=3?1:Math.Max(.01,1-(seconds-3)/.7);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(image==null)return;double scale=Math.Min(ClientSize.Width/(double)image.Width,ClientSize.Height/(double)image.Height);int w=(int)(image.Width*scale),h=(int)(image.Height*scale);e.Graphics.DrawImage(image,new Rectangle((ClientSize.Width-w)/2,(ClientSize.Height-h)/2,w,h));}
        protected override void Dispose(bool disposing){if(disposing){Complete();timer.Dispose();if(image!=null){image.Dispose();image=null;}}base.Dispose(disposing);}
    }
    public class DefenseInfo:GameForm {
        Defender defender;Label text;Timer refresh=new Timer {Interval=250};
        public DefenseInfo(Defender d){defender=d;Text=d.Pet.Name+" · 전투 능력치";FormBorderStyle=FormBorderStyle.FixedToolWindow;ShowInTaskbar=false;ClientSize=new Size(340,415);BackColor=Art.Cream;Font=new Font("맑은 고딕",9);text=new Label {Left=12,Top=12,Width=316,Height=390};Controls.Add(text);refresh.Tick+=(s,e)=>RefreshInfo();refresh.Start();RefreshInfo();Deactivate+=(s,e)=>Close();}
        void RefreshInfo(){text.Text=Describe(defender);}
        public static string Describe(Defender d){string type=d.Stats.Trajectory=="straight"?"직선":d.Stats.Trajectory=="parabolic"?"포물선":"지점 발생";return (d.Slot+1)+"번 · "+d.Pet.Name+" / "+d.Pet.Kind+"\n"+d.Pet.Personality+"\n\n공격력 "+d.AttackPower.ToString("0.0")+" · 공격 간격 "+d.AttackInterval.ToString("0.00")+"초\n공격 방식 "+type+" · 사거리 제한 없음\n\n고유 특성\n"+DefenseStats.BasicTrait(d.Pet.SpeciesId)+"\n\n고유 스킬 · "+d.Stats.Skill+"\n"+DefenseStats.SkillDetail(d.Pet.SpeciesId)+"\n스킬 기준 공격력 "+d.SkillPower.ToString("0.0")+"\n쿨타임 "+d.SkillCooldown.ToString("0.0")+"초 · 남은 "+d.SkillClock.ToString("0.0")+"초\n\n이번 판 강화\n피해 ×"+d.DamageBonus.ToString("0.00")+" / 공격속도 ×"+d.SpeedBonus.ToString("0.00")+"\n스킬 피해 ×"+d.SkillBonus.ToString("0.00")+" / 쿨타임 ×"+d.CooldownBonus.ToString("0.00");}
        protected override void Dispose(bool disposing){if(disposing){refresh.Stop();refresh.Dispose();}base.Dispose(disposing);}
    }
}
