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
                Check(Catalog.Available(first)&&Catalog.Available(alternative),"retired forms never offered");
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
            Check(Catalog.Current.Count()==22,"current roster contains 22 forms");
            var mappings=new[]{new[]{0,16},new[]{2,23},new[]{4,17},new[]{7,1},new[]{19,10},new[]{20,5},new[]{21,6},new[]{27,14},new[]{28,29}};
            foreach(var mapping in mappings){
                int retired=mapping[0],target=mapping[1];
                var data=new SaveData {StarterNutrientsClaimed=true,Seeds=99};
                var adult=new Pet {SpeciesId=retired,Name="내 고구마",Age=100,GrowthExp=180};adult.GrowthHistory.Add(retired);
                var ready=new Pet {GrowthReady=true,PendingSpecies=retired,PendingAlternative=target,GrowthExp=Engine.AdultExp};
                var dead=new Pet {SpeciesId=retired,Dead=true,Active=false};dead.GrowthHistory.Add(retired);
                data.Pets.Add(adult);data.Pets.Add(ready);data.Pets.Add(dead);data.Discovered.Add(retired);data.Discovered.Add(target);
                var loaded=Storage.Decode(Storage.Encode(data));var engine=new Engine(loaded,retired);
                Check(loaded.Pets[0].SpeciesId==target&&loaded.Pets[0].GrowthHistory.Single()==target&&loaded.Pets[2].SpeciesId==target&&loaded.Pets[2].GrowthHistory.Single()==target,"owned and dead pets and histories use specified replacement");
                Check(loaded.Discovered.SequenceEqual(new[]{target})&&loaded.Seeds==99&&loaded.Pets[0].Name==adult.Name&&loaded.Pets[0].Age==100,"discovery deduplicated without changing progress or rewards");
                var waiting=loaded.Pets[1];
                Check(waiting.PendingSpecies==target&&Catalog.Available(waiting.PendingAlternative)&&waiting.PendingAlternative!=target,"colliding pending choices keep mapped target and add distinct alternative");
                Check(!engine.Reveal(waiting,retired),"retired form cannot be chosen");
                var saved=Storage.Decode(Storage.Encode(loaded));new Engine(saved,99);
                Check(saved.Pets[1].PendingSpecies==waiting.PendingSpecies&&saved.Pets[1].PendingAlternative==waiting.PendingAlternative&&saved.Pets[0].SpeciesId==target,"replacement is stable after restart");
                Check(engine.Reveal(waiting,target)&&waiting.SpeciesId==target&&loaded.Seeds==114,"mapped discovered form grants only normal repeat reward");
            }
            var pendingData=new SaveData {StarterNutrientsClaimed=true};
            pendingData.Pets.Add(new Pet {GrowthReady=true,PendingSpecies=0,PendingAlternative=2,GrowthExp=180});
            var converted=Storage.Decode(Storage.Encode(pendingData));
            Check(converted.Pets[0].PendingSpecies==16&&converted.Pets[0].PendingAlternative==23,"both pending choices follow mapping");
            foreach(var species in Catalog.Current)Check(Catalog.Replacement(species.Id)==species.Id,"remaining IDs stay unchanged");
            return count;
        }
    }
}
