using System;
using System.Drawing;
using System.Linq;

namespace GuMaGoChi {
    public static class CarrotCombatTests {
        static int count;
        static void Check(bool condition,string message){if(!condition)throw new Exception("Carrot: "+message);count++;}
        public static int Run(){
            count=0;
            Check(Evolutions.For(26).Select(e=>e.Name).SequenceEqual(new[]{"농부마","황달마"}),"two carrot evolution choices");
            foreach(int evolution in new[]{10,11}){
                var data=new SaveData {StarterNutrientsClaimed=true};var pet=new Pet {SpeciesId=26};data.Pets.Add(pet);
                Check(Evolutions.Change(data,pet,evolution),"select carrot evolution");
                var saved=Storage.Decode(Storage.Encode(data));Check(saved.Pets[0].EvolutionId==evolution,"save retains carrot evolution");
                var battle=new DefenseBattle(data,new[]{pet},42);battle.Between=0;battle.Wave=1;battle.Remaining=1;battle.SpawnClock=100;
                var defender=battle.Team[0];defender.X=700;defender.Y=350;defender.ShotClock=0;
                var enemy=battle.Spawn(5,500,350);enemy.HP=enemy.MaxHP=1000;enemy.StopTime=100;
                battle.Step(.05);defender.ShotClock=100;var shot=battle.Shots.Single();
                Check(shot.Start.Y==(evolution==11?342:350),"spit starts at mouth and farmer throws from basket height");
                Check(shot.Damage==DefenseStats.For(26).Damage&&shot.Trajectory=="straight","existing carrot combat balance preserved");
                for(int i=0;i<20&&battle.Shots.Count>0;i++)battle.Step(.05);
                Check(enemy.HP<1000,"carrot projectile hits enemy");
                Check(battle.Skill(defender),"carrot skill available");
                Check(battle.Volleys.Single().Count==5,"five-shot skill retained");
                Check(CarrotCombatArt.ActivityAnchor(pet).Y==(evolution==11?92:115),"activity mouth anchor");
                using(var image=new Bitmap(40,40))using(var g=Graphics.FromImage(image)){
                    g.Clear(Color.Transparent);CarrotCombatArt.Projectile(g,new PointF(20,20),pet,32);
                    int green=0,orange=0;
                    for(int y=0;y<40;y++)for(int x=0;x<40;x++){Color color=image.GetPixel(x,y);if(color.A==0)continue;if(color.G>color.R)green++;if(color.R>200&&color.G>80&&color.G<220)orange++;}
                    Check(orange>0&&(evolution==10?green>0:green==0),"whole carrot has leaves; spat fragment has none");
                }
            }
            Check(CarrotCombatArt.ActivityAnchor(new Pet {SpeciesId=26})==PetWindow.BallAnchor,"base carrot ball remains unchanged");
            return count;
        }
    }
}
