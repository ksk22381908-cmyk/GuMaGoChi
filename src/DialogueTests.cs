using System;
using System.Collections.Generic;
using System.Linq;

namespace GuMaGoChi {
    public static class DialogueTests {
        static int count;
        static void Check(bool ok,string label){if(!ok)throw new Exception("DIALOGUE FAIL: "+label);count++;}
        static string Speak(Pet p,int situation) {
            if(situation<4)return Dialogue.Get(p,situation);
            switch(situation) {
                case 4:return Dialogue.Activity(p,false);
                case 5:return Dialogue.Activity(p,true);
                case 6:return Dialogue.Clean(p);
                case 7:return Dialogue.Greet(p);
                case 8:return Dialogue.Hit(p);
                case 9:return Dialogue.Skill(p,DefenseStats.For(p.SpeciesId).Line);
                default:
                    p.Illness=situation==10?30:0;p.Hunger=situation==11?70:0;p.Fatigue=situation==12?80:0;
                    return Dialogue.Need(p);
            }
        }
        public static int Run() {
            count=0;
            for(int id=-1;id<31;id++) {
                var p=new Pet {SpeciesId=id};
                for(int situation=0;situation<13;situation++) {
                    var seen=new HashSet<string>();string previous=null;
                    string need=situation==10?"치료":situation==11?"물":situation==12?"휴식":null;
                    var additions=Dialogue.Options(p,Math.Min(situation,10),"original",need).Skip(1).ToArray();
                    Check(additions.Distinct().Count()==3&&additions.All(s=>!String.IsNullOrWhiteSpace(s)&&!s.Contains("{need}")),"three distinct authored additions "+id+":"+situation);
                    for(int cycle=0;cycle<3;cycle++) {
                        var batch=new HashSet<string>();
                        for(int i=0;i<4;i++) {
                            string current=Speak(p,situation);
                            Check(current!=previous,"no consecutive repeat "+id+":"+situation);
                            if(need!=null)Check(current.Contains(need)||(id<0&&(current=="조금 아파… 도와줘."||current=="목말라! 물 마시고 싶어."||current=="졸려… 집에서 잘래.")),"need stays explicit "+id+":"+situation);
                            batch.Add(current);seen.Add(current);previous=current;
                        }
                        Check(batch.Count==4,"all four choices per cycle "+id+":"+situation);
                    }
                    Check(seen.Count==4&&additions.All(seen.Contains),"original plus all additions reachable "+id+":"+situation);
                    if(id==30)Check(seen.All(s=>s.Contains("구마")),"spy keeps authored voice "+situation);
                    if(id==2&&situation==8)Check(seen.Contains("일부러 그런 거 아니지?"),"original grapefruit hit retained");
                    if(id==30&&situation==8)Check(seen.Contains("감… 고구마 살려주구마!"),"original spy hit retained");
                    if(id==30&&situation==0)Check(seen.Contains("배가 든든하구마~"),"original spy meal retained");
                }
                var state=new SaveData();state.Pets.Add(p);p.Illness=p.Hunger=p.Fatigue=0;
                string before=Storage.Encode(state);
                Check(Dialogue.Need(p)==null,"healthy pets do not request care "+id);
                for(int i=0;i<20;i++){Dialogue.Get(p,0);Dialogue.Skill(p,DefenseStats.For(id).Line);}
                Check(Storage.Encode(state)==before,"dialogue rotation does not change save "+id);
                p.Illness=30;p.Hunger=70;p.Fatigue=80;
                string request=Dialogue.Need(p);Check(request.Contains("치료")||(id<0&&request=="조금 아파… 도와줘."),"illness takes priority "+id);
                p.Illness=0;Check(Dialogue.Need(p).Contains("물"),"water takes priority over rest "+id);
                var battle=new DefenseBattle(new SaveData(),new[]{p},42);battle.Between=0;
                battle.Marker=new System.Drawing.PointF(550,350);var defender=battle.Team[0];var bubbles=new HashSet<string>();
                for(int i=0;i<4;i++) {
                    defender.SkillClock=0;Check(battle.Skill(defender),"skill activates "+id);
                    string bubble=defender.BubbleLine;bubbles.Add(bubble);
                    Check(!battle.Skill(defender)&&defender.BubbleLine==bubble,"rejected skill keeps current bubble "+id);
                }
                Check(bubbles.Count==4,"battle skill rotates all four lines "+id);
            }
            var baby=new Pet();var idle=new HashSet<string>();
            for(int i=0;i<4;i++)idle.Add(Dialogue.Idle(baby,Engine.Hint(baby)));
            Check(idle.Count==4&&idle.Contains(Engine.Hint(baby)),"baby idle keeps growth hint alongside new speech");
            return count;
        }
    }
}
