using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace GuMaGoChi {
    public static class BerryCombatTests {
        static int count;
        static void Check(bool condition,string message){if(!condition)throw new Exception("Berry: "+message);count++;}
        static DefenseBattle Battle(int evolution){
            var pet=new Pet {SpeciesId=5,EvolutionId=evolution};var data=new SaveData {StarterNutrientsClaimed=true};data.Pets.Add(pet);
            var battle=new DefenseBattle(data,new[]{pet},42);battle.Between=0;battle.Wave=1;battle.Remaining=1;battle.SpawnClock=100;
            battle.Team[0].X=700;battle.Team[0].Y=350;battle.Team[0].ShotClock=100;return battle;
        }
        static DefenseEnemy Target(DefenseBattle battle,float x=500,float y=350){var enemy=battle.Spawn(5,x,y);enemy.HP=enemy.MaxHP=1000;enemy.StopTime=100;return enemy;}
        public static int Run(){
            count=0;Check(Catalog.All[5].Name=="베리마"&&Evolutions.For(5).Select(e=>e.Id).SequenceEqual(new[]{8,9}),"name and two evolution choices");
            var basic=Battle(-1);Check(basic.Team[0].Stats.Trajectory==DefenseStats.For(5).Trajectory,"first evolution retains original combat");
            var blue=Battle(8);var poisoned=Target(blue);blue.Team[0].ShotClock=0;blue.Step(.05);blue.Team[0].ShotClock=100;
            Check(blue.Shots.Single().Trajectory=="parabolic"&&blue.Beams.Count==0,"blueberry is a thrown projectile");
            for(int i=0;i<20&&blue.Shots.Count>0;i++)blue.Step(.05);
            Check(poisoned.PoisonTime>0&&poisoned.PoisonRate==4&&poisoned.HP<1000,"impact applies direct damage and poison");
            double before=poisoned.HP;for(int i=0;i<40;i++)blue.Step(.1);
            Check(Math.Abs(before-poisoned.HP-12)<.001&&poisoned.PoisonTime==0,"three seconds of poison has exact damage and expires");
            before=poisoned.HP;blue.Step(.1);Check(poisoned.HP==before,"expired poison never deals further damage");
            var raspberry=Battle(9);var hit=Target(raspberry);var outside=Target(raspberry,500,410);raspberry.Team[0].ShotClock=0;raspberry.Step(.05);raspberry.Team[0].ShotClock=100;
            Check(raspberry.Beams.Count==1&&raspberry.Shots.Count==0,"raspberry creates a beam without a projectile");
            double rate=raspberry.Beams[0].Rate;raspberry.Step(.1);Check(hit.HP<1000&&hit.HP>1000-rate,"beam deals damage progressively");
            for(int i=0;i<9;i++)raspberry.Step(.1);
            Check(raspberry.Beams.Count==0&&Math.Abs(1000-hit.HP-rate)<.001,"beam lasts exactly one second");
            Check(outside.HP==1000,"beam does not hit enemies outside its line");
            before=hit.HP;raspberry.Step(.1);Check(hit.HP==before,"beam expiry stops damage");
            var paused=Battle(9);Target(paused);paused.Team[0].ShotClock=0;paused.Step(.05);paused.Team[0].ShotClock=100;var beam=paused.Beams.Single();paused.Cards.Add(new DefenseCard(0,paused.Team[0]));paused.Step(.1);Check(beam.Age==0,"card selection pauses active beam");paused.Cards.Clear();paused.Finish();Check(paused.Beams.Count==0,"battle finish clears active beam");
            var fast=Battle(9);Target(fast);fast.Team[0].SpeedBonus=4;fast.Team[0].ShotClock=0;
            for(int i=0;i<40;i++){fast.Step(.05);Check(fast.Beams.Count<=1,"speed upgrades cannot overlap owner beams");}
            var partial=Battle(9);var partialHit=Target(partial);partial.Beams.Add(new DefenseBeam {Owner=partial.Team[0],Start=partial.Team[0].Point,Target=new PointF(0,350),Age=.97,Rate=20});partial.Step(.1);
            Check(Math.Abs(1000-partialHit.HP-.6)<.001,"last beam tick uses remaining duration");
            foreach(int evolution in new[]{8,9}){
                var data=new SaveData {StarterNutrientsClaimed=true};var pet=new Pet {SpeciesId=5};data.Pets.Add(pet);
                Check(Evolutions.Change(data,pet,evolution),"can select berry evolution");
                var saved=Storage.Decode(Storage.Encode(data));Check(saved.Pets[0].EvolutionId==evolution&&saved.Pets[0].Kind==Evolutions.All[evolution].Name,"saved evolution retains identity");
                Check(DefenseInfo.Describe(new Defender(pet,0)).Contains(evolution==8?"독 피해":"1초"),"combat tooltip describes evolved attack");
            }
            Render();return count;
        }
        public static void Render(){
            string[] poses={"stand","walk","eat","throw","sleep","burrow"};
            using(var image=new Bitmap(900,3*220,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(image)){
                g.Clear(Art.Cream);
                for(int row=0;row<3;row++){
                    var pet=new Pet {SpeciesId=5,EvolutionId=row==0?-1:row==1?8:9};
                    g.DrawString(pet.Kind,SystemFonts.DefaultFont,Brushes.SaddleBrown,10,row*220+4);
                    for(int col=0;col<poses.Length;col++){
                        var box=new Rectangle(col*150+20,row*220+36,110,110);pet.Sleeping=poses[col]=="sleep";pet.GrowthReady=false;
                        Check(Sprites.Draw(g,box,poses[col],poses[col]=="walk"?.25:0,false,5,pet.EvolutionId),"berry pose renders "+pet.Kind+":"+poses[col]);
                        g.DrawString(poses[col],SystemFonts.DefaultFont,Brushes.SaddleBrown,box.Left,box.Bottom+5);
                    }
                    if(row==1){BerryCombatArt.Blueberry(g,new PointF(80,row*220+192),25);var state=g.Save();g.TranslateTransform(145,row*220+192);BerryCombatArt.Poison(g,.5);g.Restore(state);}
                    if(row==2)BerryCombatArt.Beam(g,new PointF(40,row*220+195),new PointF(800,row*220+195),.5,1);
                }
                image.Save(Path.Combine(Paths.BaseDirectory,"berry-evolution-preview.png"),ImageFormat.Png);
            }
        }
    }
}
