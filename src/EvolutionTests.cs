using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace GuMaGoChi {
    public static class EvolutionTests {
        static int count;
        static void Check(bool condition,string label){if(!condition)throw new Exception("EVOLUTION FAIL: "+label);count++;}
        public static int Run() {
            count=0;string[] poses={"stand","walk","eat","throw","sleep","burrow"};
            foreach(var evolution in Evolutions.All) {
                var data=new SaveData {Seeds=123,BestGoals=9};
                var pet=new Pet {SpeciesId=evolution.Parent,Name="소중한고구마",Age=4200,GrowthExp=180,Hunger=31,Dirt=12,Fatigue=9,Illness=3,Goals=5,Shots=8,Affection=13,Play=4,Training=7};
                pet.GrowthHistory.Add(pet.SpeciesId);data.Pets.Add(pet);
                Check(Evolutions.Change(data,pet,evolution.Id),"choose evolution "+evolution.Name);
                Check(pet.Kind==evolution.Name&&pet.Personality==evolution.Personality,"evolved profile "+evolution.Id);
                Check(pet.Name=="소중한고구마"&&pet.Age==4200&&pet.GrowthExp==180&&pet.Hunger==31&&pet.Dirt==12&&pet.Fatigue==9&&pet.Illness==3&&pet.Goals==5&&pet.Shots==8&&pet.Affection==13&&pet.Play==4&&pet.Training==7&&data.Seeds==123&&data.BestGoals==9&&pet.GrowthHistory.SequenceEqual(new[]{pet.SpeciesId}),"preserve age care currency and records "+evolution.Id);
                Check(data.DiscoveredEvolutions.SequenceEqual(new[]{evolution.Id}),"register discovery "+evolution.Id);
                Check(!Evolutions.Change(data,pet,evolution.Id),"same selection no-op "+evolution.Id);
                var loaded=Storage.Decode(Storage.Encode(data));
                Check(loaded.Version==3&&loaded.Pets[0].EvolutionId==evolution.Id&&loaded.Pets[0].Kind==evolution.Name&&loaded.DiscoveredEvolutions.SequenceEqual(new[]{evolution.Id}),"evolution save roundtrip "+evolution.Id);
                var defender=new Defender(pet,0);Check(defender.Pet.EvolutionId==evolution.Id&&defender.Pet.Kind==evolution.Name&&defender.Stats.Damage==DefenseStats.For(pet).Damage,"defense retains appearance and existing stats "+evolution.Id);
                foreach(string pose in poses)for(int frame=0;frame<4;frame++)using(var image=new Bitmap(84,84,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(image)) {
                    g.Clear(Color.Transparent);double time=frame*(pose=="sleep"?.5:.25);
                    Check(Sprites.Draw(g,new Rectangle(0,0,84,84),pose,time,false,pet.SpeciesId,pet.EvolutionId),"frame loads "+evolution.Id+":"+pose+":"+frame);
                    int visible=0;for(int y=0;y<84;y++)for(int x=0;x<84;x++)if(image.GetPixel(x,y).A>0)visible++;
                    Check(visible>50&&visible<84*84*.85&&image.GetPixel(0,0).A==0&&image.GetPixel(83,0).A==0,"visible sprite without white background "+evolution.Id+":"+pose+":"+frame);
                }
                using(var image=new Bitmap(84,84))using(var g=Graphics.FromImage(image)){g.Clear(Color.Magenta);Art.Pet(g,pet,new Rectangle(0,0,84,84),0);Check(image.GetPixel(0,0).ToArgb()==Color.Magenta.ToArgb(),"Art renders evolved sprite transparently "+evolution.Id);}
                pet.Dead=true;var memory=Storage.Decode(Storage.Encode(data));Check(memory.Pets[0].Kind==evolution.Name&&memory.Pets[0].EvolutionId==evolution.Id,"album retains final form "+evolution.Id);pet.Dead=false;
                pet.Sleeping=true;Check(!Evolutions.Change(data,pet,-1),"sleep prevents change "+evolution.Id);pet.Sleeping=false;
                pet.Active=false;Check(!Evolutions.Change(data,pet,-1),"inactive prevents change "+evolution.Id);pet.Active=true;
                Check(!Evolutions.Change(data,pet,(evolution.Id+2)%Evolutions.All.Length),"wrong lineage rejected "+evolution.Id);
                Check(Evolutions.Change(data,pet,-1)&&pet.EvolutionId==-1&&data.DiscoveredEvolutions.Contains(evolution.Id),"reversible appearance retains discovery "+evolution.Id);
            }
            var old=Storage.Decode("{\"Version\":2,\"Pets\":[{\"Id\":\"legacy\",\"Name\":\"기존\",\"SpeciesId\":30,\"Age\":12345,\"GrowthExp\":180}],\"Discovered\":[30]}");
            Check(old.Pets[0].EvolutionId==-1&&old.Pets[0].Age==12345&&old.Pets[0].SpeciesId==30&&old.DiscoveredEvolutions.Count==0,"Version 2 absent fields migrate safely");
            var invalid=new SaveData();invalid.Pets.Add(new Pet {SpeciesId=23,EvolutionId=0});bool rejected=false;try{Storage.Decode(Storage.Encode(invalid));}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid lineage save rejected");
            invalid.Pets[0].EvolutionId=-2;rejected=false;try{Storage.Decode(Storage.Encode(invalid));}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid evolution sentinel rejected");
            invalid.Pets[0].EvolutionId=-1;invalid.DiscoveredEvolutions.Add(99);rejected=false;try{Storage.Decode(Storage.Encode(invalid));}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid discovery rejected");
            var baby=new Pet();var babyData=new SaveData();babyData.Pets.Add(baby);Check(!Evolutions.Change(babyData,baby,0),"baby cannot use evolution appearance");
            Render();return count;
        }
        public static void Render() {
            string[] poses={"stand","walk","eat","throw","sleep","burrow"};
            using(var image=new Bitmap(1200,Evolutions.All.Length*150))using(var g=Graphics.FromImage(image))using(var font=new Font("맑은 고딕",10)) {
                g.Clear(Art.Cream);
                for(int id=0;id<Evolutions.All.Length;id++) {
                    var evolution=Evolutions.All[id];int y=id*150;
                    g.DrawString(evolution.Name,font,Brushes.SaddleBrown,8,y+5);
                    for(int pose=0;pose<6;pose++) {
                        int x=pose*200;g.DrawString(poses[pose],font,Brushes.SaddleBrown,x+8,y+27);
                        for(int frame=0;frame<4;frame++) {
                            var box=new Rectangle(x+frame*49+2,y+53,47,84);
                            Sprites.Draw(g,box,poses[pose],frame*(poses[pose]=="sleep"?.5:.25),false,evolution.Parent,id);
                        }
                    }
                }
                image.Save(Path.Combine(Paths.BaseDirectory,"evolutions-preview.png"));
            }
        }
    }
}
