using System.Drawing;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class GrowthChoiceDialog:GameForm {
        public int SelectedSpecies {get;private set;}
        public GrowthChoiceDialog(Pet pet) {
            SelectedSpecies=-1;
            Text=pet.Name+" · 1차 진화 선택";ClientSize=new Size(560,420);
            AutoScaleMode=AutoScaleMode.Font;FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterScreen;MaximizeBox=false;MinimizeBox=false;
            TopMost=true;BackColor=Art.Cream;Font=new Font("맑은 고딕",10);
            Controls.Add(new Label {Text="어떤 고구마로 자랄까요? 두 모습 중 하나를 골라 주세요.",Left=20,Top=16,Width=520,Height=36});
            var choices=new[]{pet.PendingSpecies,pet.PendingAlternative};
            for(int i=0;i<choices.Length;i++) {
                int id=choices[i];var species=Catalog.All[id];int x=20+i*270;
                Controls.Add(new CollectionCard(id,species.Name,species.Personality+"\n특기: "+species.Skill) {Left=x,Top=58,Width=250,Height=218});
                var choose=new Button {Text=species.Name+"로 진화",Left=x,Top=284,Width=250,Height=40};
                choose.Click+=(s,e)=>{SelectedSpecies=id;DialogResult=DialogResult.OK;Close();};Controls.Add(choose);
            }
            var later=UiLayout.DialogButton("나중에 선택");later.DialogResult=DialogResult.Cancel;
            Controls.Add(UiLayout.DialogActions(later));CancelButton=later;
            AutoScaleDimensions=CurrentAutoScaleDimensions;
        }
    }
}
