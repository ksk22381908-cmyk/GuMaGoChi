using System;
using System.Drawing;
using System.Linq;

namespace GuMaGoChi {
    public static class RoyalCombatTests {
        static int count;
        static void Check(bool value,string message){if(!value)throw new Exception("Royal: "+message);count++;}
        static DefenseBattle Battle(int evolution){
            var data=new SaveData {StarterNutrientsClaimed=true};var pet=new Pet {SpeciesId=10,EvolutionId=evolution};data.Pets.Add(pet);
            var b=new DefenseBattle(data,new[]{pet},42);b.Between=0;b.Wave=1;b.Remaining=1;b.SpawnClock=100;
            b.Team[0].X=700;b.Team[0].Y=350;b.Team[0].ShotClock=100;return b;
        }
        static DefenseEnemy Enemy(DefenseBattle b,float x,float y=350){var e=b.Spawn(0,x,y);e.HP=e.MaxHP=10000;e.StopTime=100;return e;}
        public static int Run(){
            count=0;Check(Catalog.All[10].Name=="왕구마","base king name");
            Check(Evolutions.For(10).Select(e=>e.Name).SequenceEqual(new[]{"왕고구마","망했구마"}),"king evolution names and lineage");
            var king=Battle(12);var d=king.Team[0];var far=Enemy(king,400);d.ShotClock=0;king.Step(.05);
            Check(!king.InRange(d,far.Point)&&king.Pulses.Count==0,"king cannot attack beyond short range");
            far.Dead=true;var near=Enemy(king,600);var splash=Enemy(king,615,370);var outside=Enemy(king,600,450);
            d.ShotClock=0;king.Step(.05);d.ShotClock=100;
            Check(king.Pulses.Count==1&&king.Shots.Count==0&&near.HP==10000,"king winds up a target-area pulse without a projectile");
            for(int i=0;i<10;i++)king.Step(.05);
            Check(near.HP==10000-48&&splash.HP==10000-48&&outside.HP==10000,"small pulse hits only its area after windup");
            Check(d.Stats.Interval>DefenseStats.For(10).Interval&&d.AttackPower>DefenseStats.For(10).Damage,"king attacks slower and harder");
            foreach(int evolution in new[]{12,13}){
                var battle=Battle(evolution);var defender=battle.Team[0];var target=Enemy(battle,600);var neighbor=Enemy(battle,550,420);var excluded=Enemy(battle,350);
                target.StopTime=neighbor.StopTime=excluded.StopTime=0;
                battle.ToggleMarker(target.Point);
                Check(battle.Skill(defender)&&!battle.Skill(defender),"skill starts cooldown and cannot repeat");
                Check(target.HP==10000&&battle.Pulses.Single().Stun==2,"skill damage waits for visible impact");
                for(int i=0;i<5;i++)battle.Step(.1);
                double damage=defender.SkillPower*(evolution==12?3:2.5);
                Check(Math.Abs(target.HP-(10000-damage))<.001&&neighbor.HP<10000&&excluded.HP==10000,"wide skill damage respects area boundary");
                Check(target.StopTime>0&&neighbor.StopTime>0&&excluded.StopTime==0,"both skills stun enemies in area");
                Check(battle.Shield==0,"evolved skills replace original king shield");
                var paused=Battle(evolution);Enemy(paused,600);paused.Skill(paused.Team[0]);var pulse=paused.Pulses.Single();paused.Cards.Add(new DefenseCard(0,paused.Team[0]));paused.Step(.1);
                Check(pulse.Left==.5,"card choice pauses pending skill impact");paused.Finish();Check(paused.Pulses.Count==0,"finish cancels pending damage");
            }
            var beggar=Battle(13);var b=beggar.Team[0];var first=Enemy(beggar,600);b.ShotClock=0;beggar.Step(.05);
            Check(beggar.Shots.Single().Trajectory=="straight"&&b.RapidStacks==0,"beggar starts straight trash attack without stacks");
            double original=b.AttackInterval;
            for(int i=0;i<15;i++){b.ShotClock=0;beggar.Step(.01);}
            Check(b.RapidStacks==10&&Math.Abs(b.AttackInterval-original/2)<.0001,"same target ramps speed to capped double rate");
            var second=Enemy(beggar,650);b.ShotClock=0;beggar.Step(.01);
            Check(b.RapidTarget==second.Id&&b.RapidStacks==0&&b.AttackInterval==original,"new target resets attack speed");
            second.Dead=true;first.Dead=true;beggar.Step(.01);Check(b.RapidTarget==-1&&b.RapidStacks==0,"target death clears acceleration");
            var marked=Battle(13);Enemy(marked,600);marked.ToggleMarker(new PointF(200,100));marked.Team[0].ShotClock=0;marked.Step(.01);
            Check(marked.Team[0].RapidStacks==0&&marked.Team[0].RapidTarget==-1,"empty marker cannot stack target acceleration");
            var immune=Battle(13);var boss=immune.Spawn(5,600,350);boss.HP=10000;boss.StopTime=0;immune.Skill(immune.Team[0]);for(int i=0;i<5;i++)immune.Step(.1);
            Check(boss.StopTime>0&&boss.StopTime<=1,"boss stun uses existing half-duration rule");
            using(var image=new Bitmap(128,128))using(var g=Graphics.FromImage(image)){
                foreach(string pose in new[]{"stand","walk","eat","throw","sleep","burrow"})for(int frame=0;frame<4;frame++)Check(Sprites.Draw(g,new Rectangle(0,0,128,128),pose,frame*(pose=="sleep"?.5:.25),false,10),"base king frame "+pose+frame);
            }
            return count;
        }
    }
}
