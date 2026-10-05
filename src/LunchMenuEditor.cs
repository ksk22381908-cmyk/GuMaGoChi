using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class LunchMenuEditor:Form {
        readonly TextBox input;readonly Label count;public List<string> Menus;bool defaults;
        public static List<string> Parse(string text){return text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(m=>m.Trim()).Where(m=>m.Length>0).Distinct().ToList();}
        public LunchMenuEditor(List<string> saved){Text="내 점심 메뉴 목록";ClientSize=new Size(500,540);Font=new Font("맑은 고딕",10);StartPosition=FormStartPosition.CenterParent;MinimizeBox=MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;
            Controls.Add(new Label {Text="한 줄에 메뉴 하나씩 입력하거나 붙여 넣으세요.\n식당 이름도 가능: 회사 앞 분식 · 김밥 / 골목 국밥",Bounds=new Rectangle(15,15,470,48)});
            input=new TextBox {Bounds=new Rectangle(15,70,470,370),Multiline=true,AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,MaxLength=20000,Text=String.Join(Environment.NewLine,saved.Count>0?saved:LunchPlan.Pool.ToList())};Controls.Add(input);count=new Label {Bounds=new Rectangle(15,449,470,26)};Controls.Add(count);
            input.TextChanged+=(s,e)=>{defaults=false;UpdateCount();};var restore=new Button {Text="기본 41개로 되돌리기",Bounds=new Rectangle(15,490,190,35)};restore.Click+=(s,e)=>{input.Text=String.Join(Environment.NewLine,LunchPlan.Pool);defaults=true;UpdateCount();};Controls.Add(restore);
            var save=new Button {Text="저장",Bounds=new Rectangle(290,490,90,35)};save.Click+=(s,e)=>{var parsed=Parse(input.Text);if(parsed.Count<5||parsed.Count>200||parsed.Any(m=>m.Length>40)){MessageBox.Show(this,"서로 다른 메뉴 5~200개를 입력하세요. 메뉴 하나는 40자까지입니다.");return;}Menus=defaults?new List<string>():parsed;DialogResult=DialogResult.OK;Close();};Controls.Add(save);var cancel=new Button {Text="취소",Bounds=new Rectangle(390,490,90,35),DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;UpdateCount();}
        void UpdateCount(){count.Text=Parse(input.Text).Count+"개 메뉴 · 빈 줄과 중복 메뉴는 자동 제외";}
    }
}
