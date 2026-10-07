using System;
using System.IO;
using System.Linq;

namespace GuMaGoChi {
    public static class GrowthChoiceTests {
        static int count;
        static void Check(bool ok,string message){if(!ok)throw new Exception("Growth choices: "+message);count++;}
        public static int Run(){
            count=0;
            for(int seed=0;seed<100;seed++)foreach(bool second in new[]{false,true}){
                var data=new SaveData {StarterNutrientsClaimed=true};var engine=new Engine(data,seed);
                var pet=engine.Adopt("선택 테스트",true);pet.Training=12;pet.GrowthExp=Engine.AdultExp-1.0/60;
                engine.Tick(1);int first=pet.PendingSpecies,alternative=pet.PendingAlternative;
                Check(pet.GrowthReady&&first>=0&&alternative>=0&&first!=alternative,"two distinct candidates");
                Check(first!=6&&alternative!=6,"both candidates respect training exclusion");
                engine.Tick(60);Check(pet.PendingSpecies==first&&pet.PendingAlternative==alternative,"waiting keeps candidates");
                var loaded=Storage.Decode(Storage.Encode(data));var resumed=new Engine(loaded,seed+100);
                var ready=loaded.Pets[0];resumed.EnsureGrowthChoices(ready);
                Check(ready.PendingSpecies==first&&ready.PendingAlternative==alternative,"save and reopen keep candidates");
                int invalid=Catalog.All.First(s=>s.Id!=first&&s.Id!=alternative).Id;
                Check(!resumed.Reveal(ready,invalid)&&ready.GrowthReady&&loaded.Seeds==0,"unoffered choice rejected");
                int selected=second?alternative:first;
                Check(resumed.Reveal(ready,selected)&&ready.SpeciesId==selected&&!ready.GrowthReady,"either offered form selectable");
                Check(ready.PendingSpecies==-1&&ready.PendingAlternative==-1&&ready.GrowthHistory.SequenceEqual(new[]{selected})&&loaded.Discovered.SequenceEqual(new[]{selected})&&loaded.Seeds==30,"only chosen form recorded and rewarded");
                Check(!resumed.Reveal(ready,selected)&&loaded.Seeds==30,"no repeated reward");
                Check(Storage.Decode(Storage.Encode(loaded)).Pets[0].SpeciesId==selected,"chosen form survives save");
            }
            var legacy=Storage.Decode("{\"Version\":3,\"StarterNutrientsClaimed\":true,\"Pets\":[{\"GrowthReady\":true,\"PendingSpecies\":23,\"GrowthExp\":180}]}");
            new Engine(legacy,17);var old=legacy.Pets[0];int extra=old.PendingAlternative;
            Check(old.PendingSpecies==23&&extra>=0&&extra!=23,"old pending result retained with second candidate");
            new Engine(legacy,22);Check(old.PendingAlternative==extra,"migration does not redraw");
            old.PendingAlternative=old.PendingSpecies;bool rejected=false;
            try{Storage.Decode(Storage.Encode(legacy));}catch(InvalidDataException){rejected=true;}
            Check(rejected,"duplicate saved candidate rejected");
            return count;
        }
    }
}
