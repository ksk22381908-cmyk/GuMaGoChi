using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GuMaGoChi {
    public class HomeWindow:Form {
        DesktopApp app;ListBox pets;Label header,detail,message;Button actions,adopt;
        TabControl tabs;Habitat habitat;FlowLayoutPanel dex,album;Label bag;
        public HomeWindow(DesktopApp owner) {
            app=owner;Text="GuMaGoChi · 땅속 고구마 집";ClientSize=new Size(1000,720);MinimumSize=new Size(860,650);StartPosition=FormStartPosition.CenterScreen;BackColor=Art.Cream;Font=new Font("맑은 고딕",10);DoubleBuffered=true;
            header=new Label {Left=24,Top=18,Width=940,Height=40,Font=new Font("맑은 고딕",17,FontStyle.Bold),ForeColor=Art.Ink};Controls.Add(header);
            var note=new Label {Left=24,Top=57,Width=940,Height=25,Text="집 창을 닫아도 활성 고구마는 계속 생활해요. 멈추려면 비활성화 또는 트레이의 ‘잠시 쉬기’를 선택하세요.",ForeColor=Art.Ink};Controls.Add(note);
            tabs=new TabControl {Left=20,Top=94,Width=960,Height=555,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};Controls.Add(tabs);
            var home=new TabPage("땅속 집 · 돌봄") {BackColor=Art.Cream};tabs.TabPages.Add(home);
            pets=new ListBox {Left=12,Top=16,Width=235,Height=210,Font=new Font("맑은 고딕",10),DisplayMember="Text"};pets.SelectedIndexChanged+=(s,e)=>RefreshDetail();home.Controls.Add(pets);
            actions=new Button {Text="선택한 고구마의 행동 메뉴 ▾",Left=12,Top=235,Width=235,Height=36};actions.Click+=(s,e)=>{Pet p=Selected();if(p!=null)app.MenuFor(p).Show(actions,new Point(0,actions.Height));};home.Controls.Add(actions);
            adopt=new Button {Text="새 아기 입양",Left=12,Top=280,Width=235,Height=36};adopt.Click+=(s,e)=>app.Adopt();home.Controls.Add(adopt);
            detail=new Label {Left=12,Top=331,Width=235,Height=180,ForeColor=Art.Ink};home.Controls.Add(detail);
            habitat=new Habitat(app) {Left=260,Top=16,Width=672,Height=340,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};home.Controls.Add(habitat);home.Resize+=(s,e)=>{habitat.Width=Math.Max(200,home.ClientSize.Width-habitat.Left-20);};
            var help=new Label {Left=274,Top=373,Width=640,Height=70,ForeColor=Art.Ink,Text="고구마 우클릭: 먹이·치료·수면·외출\n배설물 클릭: 직접 청소 / 흙더미 클릭: 성장 공개\n목록에서 비활성 개체도 선택할 수 있어요. 수치는 낮을수록 양호해요."};home.Controls.Add(help);
            var shop=new TabPage("씨앗 상점 · 공용 가방") {BackColor=Art.Cream};tabs.TabPages.Add(shop);
            bag=new Label {Left=28,Top=25,Width=850,Height=90,Font=new Font("맑은 고딕",14,FontStyle.Bold)};shop.Controls.Add(bag);
            AddButton(shop,"아침 이슬 구매 · 씨앗 8개\n배고픔 −30 / 1개",30,140,260,80,()=>app.Change(()=>ShowMessage(app.Engine.Buy(false)?"아침 이슬 1개를 가방에 넣었어요.":"씨앗이 부족해요.")));
            AddButton(shop,"상위 치료 구매 · 씨앗 12개\n병세 −30 / 1개",320,140,260,80,()=>app.Change(()=>ShowMessage(app.Engine.Buy(true)?"상위 치료 1개를 가방에 넣었어요.":"씨앗이 부족해요.")));
            AddButton(shop,"새 아기 입양 · 씨앗 100개",610,140,290,80,()=>app.Adopt());
            shop.Controls.Add(new Label {Left=30,Top=255,Width=860,Height=115,Text="무료 수돗물과 기본 치료는 개체 행동 메뉴에서 사용해요.\n먹이는 개체별 5분, 치료는 10분의 공통 쿨타임이 있어요.\n가방 아이템은 모든 고구마가 공유하지만, 효과는 사용한 한 마리에게만 적용돼요.\n반복 놀이·훈련은 가능하며, 성장 기록과 씨앗 보상은 20분 간격으로 쌓여요."});
            var collection=new TabPage("성체 도감") {BackColor=Art.Cream};tabs.TabPages.Add(collection);dex=Flow(collection);
            var memories=new TabPage("추억 앨범") {BackColor=Art.Cream};tabs.TabPages.Add(memories);album=Flow(memories);
            var settings=new TabPage("저장 · 사용 안내") {BackColor=Art.Cream};tabs.TabPages.Add(settings);
            settings.Controls.Add(new Label {Left=26,Top=25,Width=875,Height=210,Text="자동 저장: 돌봄·입양·구매·성장 직후 및 활성 실행 중 30초마다\n저장 위치: "+Storage.Folder+"\n이전 저장본은 save.json.bak으로 유지해요.\n\n수명은 활성 실행 시간 100시간, 성체 성장은 8시간이에요.\n수면은 시간을 포함하고, 비활성·성장 대기·절전·잠금·전체 화면 중에는 멈춰요.\n3단계 성장과 집 꾸미기는 추후 업데이트 예정이에요.\n아기 이동·식사·발사·성장 준비 모션을 제공해요. 대기·수면은 기본 표현이에요."});
            AddButton(settings,"저장 데이터 내보내기",28,260,230,42,Export);
            AddButton(settings,"저장 데이터 가져오기",278,260,230,42,Import);
            AddButton(settings,"모두 잠시 쉬기 / 다시 시작",528,260,280,42,()=>{app.ManualPause=!app.ManualPause;app.SyncWindows();if(app.Activity!=null)app.Activity.SetPaused(app.Paused);RefreshData();});
            AddButton(settings,"프로그램 종료 · 저장",28,330,230,42,app.Exit);
            message=new Label {Left=24,Top=666,Width=940,Height=32,Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,ForeColor=Art.Green};Controls.Add(message);
            FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
            tabs.SelectedIndexChanged+=(s,e)=>RefreshData();RefreshData();
        }
        FlowLayoutPanel Flow(Control parent) {var panel=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(15),BackColor=Art.Cream};parent.Controls.Add(panel);return panel;}
        void AddButton(Control parent,string text,int x,int y,int width,int height,Action action) {var b=new Button {Text=text,Left=x,Top=y,Width=width,Height=height,BackColor=Color.FromArgb(226,235,206),ForeColor=Art.Ink,FlatStyle=FlatStyle.Flat};b.Click+=(s,e)=>action();parent.Controls.Add(b);}
        class PetEntry {public Pet Pet;public string Text {get{return (Pet.Active?"● ":"○ ")+Pet.Name+" · "+(Pet.GrowthReady?"성장 대기":Pet.Sleeping?"수면":Pet.Home?"집":"바탕화면");}}}
        public Pet Selected() {return pets.SelectedItem is PetEntry?((PetEntry)pets.SelectedItem).Pet:null;}
        public void SelectPet(string id) {tabs.SelectedIndex=0;for(int i=0;i<pets.Items.Count;i++)if(((PetEntry)pets.Items[i]).Pet.Id==id){pets.SelectedIndex=i;break;}}
        public void ShowMessage(string text) {message.Text=text;}
        public void AnimateMeal(Pet p) {habitat.AnimateMeal(p);}
        void RefreshDetail() {
            Pet p=Selected();actions.Enabled=p!=null;if(p==null){detail.Text="함께할 고구마를 입양해 보세요.";return;}
            detail.Text=p.Name+" · "+p.Kind+"\n"+p.Personality+" / "+p.Skill+"\n나이 "+(p.Age/3600).ToString("0.00")+" / 100시간\n배고픔 "+Math.Round(p.Hunger)+" · 청결 "+Math.Round(p.Dirt)+"\n피로 "+Math.Round(p.Fatigue)+" · 병세 "+Math.Round(p.Illness)+"\n"+(p.SpeciesId<0?Engine.Hint(p):"추가 성장: 추후 업데이트")+"\n교감 "+p.Affection+" / 놀이 "+p.Play+" / 훈련 "+p.Training;
        }
        int dexCount=-1,deadCount=-1;
        public void RefreshData() {
            if(IsDisposed)return;header.Text="작은 땅속 집  ·  씨앗 "+app.Engine.Data.Seeds+"개"+(app.Paused?"  [육성 일시정지]":"");
            string selected=Selected()==null?null:Selected().Id;var living=app.Engine.Data.Pets.Where(p=>!p.Dead).ToArray();
            pets.BeginUpdate();pets.Items.Clear();foreach(Pet p in living)pets.Items.Add(new PetEntry {Pet=p});pets.EndUpdate();
            if(selected!=null){for(int i=0;i<pets.Items.Count;i++)if(((PetEntry)pets.Items[i]).Pet.Id==selected){pets.SelectedIndex=i;break;}}
            if(pets.SelectedIndex<0&&pets.Items.Count>0)pets.SelectedIndex=0;RefreshDetail();habitat.Invalidate();
            bool rescue=living.Length==0&&app.Engine.Data.Seeds<Engine.AdoptPrice;adopt.Text=rescue?"무료 아기 입양 · 다시 시작":"새 아기 입양 · 씨앗 100개";
            bag.Text="씨앗 "+app.Engine.Data.Seeds+"개\n공용 가방: 아침 이슬 "+app.Engine.Data.Dew+"개 / 상위 치료 "+app.Engine.Data.Medicine+"개";
            if(dexCount!=app.Engine.Data.Discovered.Count) {
                foreach(Control c in dex.Controls.Cast<Control>().ToArray())c.Dispose();dex.Controls.Clear();
                foreach(Species s in Catalog.All) {bool found=app.Engine.Data.Discovered.Contains(s.Id);dex.Controls.Add(new CollectionCard(found?s.Id:-2,found?s.Name:"???",found?s.Personality+"\n특기: "+s.Skill+"\n추가 성장: 추후 업데이트":"아직 만나지 못했어요.") {Width=205,Height=235,Margin=new Padding(7)});}dexCount=app.Engine.Data.Discovered.Count;
            }
            var dead=app.Engine.Data.Pets.Where(p=>p.Dead).Reverse().ToArray();if(deadCount!=dead.Length) {
                foreach(Control c in album.Controls.Cast<Control>().ToArray())c.Dispose();album.Controls.Clear();
                if(dead.Length==0)album.Controls.Add(new Label {Text="함께한 고구마의 추억이 이곳에 남아요.",Width=750,Height=40});
                foreach(Pet p in dead)album.Controls.Add(new CollectionCard(p.SpeciesId,p.Name+" · "+p.Kind,p.Personality+" / "+p.Skill+"\n함께한 시간 "+(p.Age/3600).ToString("0.00")+"시간\n"+p.DiedAt+" · "+p.Cause+"\n교감 "+p.Affection+" / 놀이 "+p.Play+" / 훈련 "+p.Training+"\n골인 "+p.Goals+" / 시도 "+p.Shots+"\n성장: "+(p.GrowthHistory.Count==0?"아기":String.Join(" → ",p.GrowthHistory.Select(id=>Catalog.All[id].Name)))) {Width=280,Height=305,Margin=new Padding(7)});deadCount=dead.Length;
            }
        }
        void Export() {using(var d=new SaveFileDialog {Filter="고구마 저장 파일 (*.json)|*.json",FileName="GuMaGoChi-backup-"+DateTime.Now.ToString("yyyyMMdd")+".json"})if(d.ShowDialog()==DialogResult.OK)try{app.Save();File.WriteAllText(d.FileName,Storage.Encode(app.Engine.Data),System.Text.Encoding.UTF8);ShowMessage("저장 데이터를 내보냈어요.");}catch(Exception ex){MessageBox.Show(ex.Message,"내보내기 실패");}}
        void Import() {
            using(var d=new OpenFileDialog {Filter="고구마 저장 파일 (*.json)|*.json"})if(d.ShowDialog()==DialogResult.OK)try {
                var data=Storage.Decode(File.ReadAllText(d.FileName));
                if(MessageBox.Show("현재 육성 데이터를 가져온 파일로 교체합니다. 현재 데이터는 별도 백업합니다. 계속할까요?","저장 데이터 가져오기",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
                Directory.CreateDirectory(Storage.Folder);File.WriteAllText(Path.Combine(Storage.Folder,"before-import-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json"),Storage.Encode(app.Engine.Data));
                if(app.Activity!=null)app.Activity.CancelActivity();
                // Auto-start belongs to this PC, not to an imported save.
                data.AutoStart=app.Engine.Data.AutoStart;app.Engine=new Engine(data);dexCount=-1;deadCount=-1;app.Change(()=>{});ShowMessage("저장 데이터를 가져왔어요.");
            }catch(Exception ex){MessageBox.Show(ex.Message,"가져오기 실패");}
        }
    }
    public class Habitat:Control {
        DesktopApp app;
        Timer animationTimer;double phase=0;Dictionary<string,double> meals=new Dictionary<string,double>(),growth=new Dictionary<string,double>();
        public Habitat(DesktopApp owner) {app=owner;DoubleBuffered=true;MouseDown+=ClickPet;animationTimer=new Timer {Interval=125};animationTimer.Tick+=(s,e)=>{if(!app.Paused){phase+=.125;if(Visible)Invalidate();}};animationTimer.Start();Disposed+=(s,e)=>{animationTimer.Stop();animationTimer.Dispose();};}
        public void AnimateMeal(Pet p) {meals[p.Id]=phase;Invalidate();}
        Pet[] Residents {get{return app.Engine.Data.Pets.Where(p=>p.Home&&p.Active&&!p.Dead).ToArray();}}
        Rectangle Slot(int i) {int columns=Math.Max(1,(int)(Width*.65)/120);return new Rectangle((int)(Width*.18)+(i%columns)*120,(int)(Height*.78)-110+(i/columns)*160,110,110);}
        void ClickPet(object s,MouseEventArgs e) {
            var list=Residents;for(int i=0;i<list.Length;i++) {var r=Slot(i);if(r.Contains(e.Location)) {Pet p=list[i];if(e.Button==MouseButtons.Right)app.MenuFor(p).Show(this,e.Location);else if(p.GrowthReady)app.Reveal(p);else app.Say(p,p.Name+"가 집에서 쉬고 있어요.");return;}if(new Rectangle(r.X,r.Bottom+16,100,25).Contains(e.Location)&&e.Button==MouseButtons.Left&&!app.Paused) {Pet p=list[i];app.Change(()=>{if(app.Engine.Clean(p))app.Say(p,"잠자리도 깨끗해졌어요!");});return;}}
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);Graphics g=e.Graphics;g.Clear(Color.FromArgb(109,75,49));
            if(Sprites.Home!=null){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.DrawImage(Sprites.Home,new Rectangle(0,0,Width,Height),0,0,Sprites.Home.Width,Sprites.Home.Height,GraphicsUnit.Pixel);}
            else {using(var b=new SolidBrush(Color.FromArgb(173,126,78)))g.FillRectangle(b,8,24,Width-16,Height-32);}
            var list=Residents;
            if(list.Length==0)TextRenderer.DrawText(g,"지금은 집이 조용해요.\n고구마 메뉴에서 ‘집에 가기’를 선택해 보세요.",Font,new Rectangle(30,120,Width-60,100),Art.Cream,TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak);
            for(int i=0;i<list.Length;i++) {Rectangle r=Slot(i);if(r.Top>Height-80)break;Pet p=list[i];double start;string key="stand";double time=phase;if(p.GrowthReady){if(!growth.TryGetValue(p.Id,out start)){start=phase;growth[p.Id]=start;}time=phase-start;}else if(meals.TryGetValue(p.Id,out start)&&phase-start<2){key="eat";time=phase-start;}Art.Pet(g,p,r,time,key);TextRenderer.DrawText(g,p.Name+(p.Sleeping?" · 수면":""),Font,new Rectangle(r.X,r.Bottom,120,20),Art.Cream,TextFormatFlags.HorizontalCenter);
                if(p.Waste>0) {Art.Waste(g,new Rectangle(r.X,r.Bottom+21,27,20));TextRenderer.DrawText(g,"치우기 · "+p.Waste,Font,new Point(r.X+30,r.Bottom+20),Art.Cream);}
            }
            if(list.Length>Math.Max(1,Width/135))TextRenderer.DrawText(g,"더 많은 개체는 왼쪽 목록에서 선택해 주세요.",Font,new Point(15,Height-26),Art.Cream);
        }
    }
    public class CollectionCard:Control {
        int id;string title,description;
        public CollectionCard(int species,string name,string text) {id=species;title=name;description=text;DoubleBuffered=true;BackColor=Color.FromArgb(244,232,207);Font=new Font("맑은 고딕",9);}
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);Graphics g=e.Graphics;using(var p=new Pen(Color.FromArgb(196,179,145)))g.DrawRectangle(p,0,0,Width-1,Height-1);
            if(id>= -1)Art.Pet(g,new Pet {SpeciesId=id},new Rectangle((Width-100)/2,8,100,100),0);else TextRenderer.DrawText(g,"?",new Font("맑은 고딕",35,FontStyle.Bold),new Rectangle(0,15,Width,80),Color.FromArgb(167,150,127),TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g,title,new Font("맑은 고딕",11,FontStyle.Bold),new Rectangle(8,114,Width-16,24),Art.Ink,TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g,description,Font,new Rectangle(12,147,Width-24,Height-153),Art.Ink,TextFormatFlags.WordBreak);
        }
    }
}
