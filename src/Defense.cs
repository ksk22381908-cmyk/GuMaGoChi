using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace GuMaGoChi {
    public class DefenseStats {
        public int Damage,Range;public double Interval,Cooldown;
        public string Trajectory,Skill,Line;
        static readonly int[] damage={20,14,25,32,12,15,28,16,25,19,36,22,34,21,18,14,24,16,25,40,14,16,17,17,13,10,35,23,18,13,23};
        static readonly int[] range={550,500,650,500,500,550,650,550,550,600,500,650,600,550,600,600,650,650,700,650,550,550,550,650,450,450,600,650,550,550,600};
        static readonly double[] interval={1,1.1,1.2,1.6,.6,1,1.8,1.1,1.3,1,1.8,1.4,1.7,1.1,.9,1,1.2,.8,1.4,2,1,1.2,1,.7,.7,.45,1.7,1.2,.9,.7,1.3};
        static readonly int[] cooldown={25,25,25,30,25,30,30,35,30,25,30,30,30,30,30,35,30,25,35,35,35,30,25,25,30,25,30,30,25,30,30};
        static readonly string[] skills={"성실한 한 방","끈적한 꿀밭","딱히 도와주는 건 아냐","뜨끈한 군밤","쑥쑥 연사","나 좀 봐!","낮잠 구름","포근한 보호막","햇살 응원","발끈 눈보라","왕의 호통","고구마 대폭발","고구마 함포 사격","껍질 손질","색칠 완료","앙코르!","약점은 여기야","길을 뚫자!","작은 블랙홀","진짜 별똥별!","다정한 꽃밭","포자 구름","끈적한 개굴 점액","거기, 빈틈","누가 들어오래!","깜짝 토끼 연사","끝까지 던진다","정신 차려","승부는 지금부터","무지개 소나기","고구마 폭탄이구마!"};
        static readonly string[] lines={"차근차근… 제대로 한 방!","서두르지 마~ 끈적하니까!","딱히 널 도와주는 건 아니거든!","뜨거우니까 조심해!","쑥쑥! 더 빠르게!","다들 나 보고 힘내!","졸리니까… 잠깐 멈춰…","괜찮아. 내가 지켜줄게.","얘들아, 힘내! 할 수 있어!","아 진짜! 꽁꽁 얼어버려!","무… 물러서거라!","이 정도 마법이면 충분하겠지.","전 포문, 발사!","껍질부터 손질하자!","거기 그대로! 색칠 끝낼게!","한 번 더! 앙코르!","내가 읽었지. 약점은 여기야!","앞으로! 길을 뚫자!","중력은 내 편이야.","봤지? 이게 진짜 별똥별이다!","우리 집엔 꽃길만!","내 속도로… 퍼져라.","개굴! 발밑 조심해!","흥. 빈틈투성이네.","야! 누가 들어오래!","오지 마! 오지 말라니까!","끝까지! 하나 더!","정신 차려. 여긴 네 자리가 아냐.","좋아. 이제 승부 보자.","이번엔 전부 쏟아볼까!","감… 고구마 폭탄이구마!"};
        public static DefenseStats For(int id){if(id<0)return new DefenseStats {Damage=6,Range=450,Interval=1.35,Cooldown=20,Trajectory="straight",Skill="흙장난",Line="흙장난이다!"};if(id>=31)throw new ArgumentOutOfRangeException("id");return new DefenseStats {Damage=Math.Max(1,(int)Math.Round(damage[id]*.65)),Range=range[id],Interval=interval[id]*1.35,Cooldown=cooldown[id],Trajectory=new[]{1,3,12,20,21,22,30}.Contains(id)?"parabolic":new[]{6,11,18,19}.Contains(id)?"point":"straight",Skill=skills[id],Line=lines[id]};}
        public static DefenseStats For(Pet pet){
            var stats=For(pet.SpeciesId);
            if(pet.SpeciesId==5&&pet.EvolutionId==8){stats.Trajectory="parabolic";stats.Interval=1.6;}
            if(pet.SpeciesId==5&&pet.EvolutionId==9){stats.Trajectory="beam";stats.Interval=2;}
            return stats;
        }
        public static string BasicTrait(Pet pet){
            if(pet.SpeciesId==5&&pet.EvolutionId==8)return "블루베리를 던져 명중 피해 + 3초 동안 초당 4의 독 피해. 재명중은 독 시간을 갱신해요.";
            if(pet.SpeciesId==5&&pet.EvolutionId==9)return "라즈베리 즙 빔을 1초 동안 발사. 빔 위의 적에게 총 공격력 2배의 지속 피해를 줘요.";
            return BasicTrait(pet.SpeciesId);
        }
        public static string BasicTrait(int id){switch(id){case -1:return "작은 흙덩이로 공격하는 아기. 성체보다 약해요.";case 0:return "같은 적을 연속 공격하면 피해가 최대 20% 증가.";case 1:return "적을 1초 동안 15% 느리게 해요.";case 2:case 13:case 27:return "딱정벌레 방어력의 절반을 무시해요.";case 3:return "명중 지점 가까운 다른 적에도 50% 피해.";case 9:return "적을 1초 동안 20% 느리게 해요.";case 14:return "2초 동안 적이 받는 피해를 10% 증가.";case 16:return "거대 버러지에게 기본 공격 피해 20% 증가.";case 17:return "기본 공격이 최대 두 적을 관통해요.";case 21:return "명중하면 3초 동안 초당 4의 독 피해.";case 22:return "근처 다른 적에게 50% 피해로 한 번 튕겨요.";case 28:return "네 번째 기본 공격마다 피해가 두 배.";default:return "캐릭터 전용 이펙트로 공격해요.";}}
        static readonly string[] skillDetails={"8초 동안 자신의 공격 간격을 30% 단축.","5초 동안 꿀밭 안의 적 이동 속도 40% 감소.","공격력 4배의 관통탄. 최대 세 적과 방어력 관통.","공격력 3배 폭발 + 4초 동안 초당 공격력 50% 화상.","5초 동안 자신의 공격 간격을 절반으로 단축.","6초 동안 전체 기본 공격력 20% 증가.","범위 내 적 1초 정지 + 3초 동안 40% 감속.","8초 동안 방어선에 피해 10을 막는 보호막.","6초 동안 전체 기본 공격 간격 25% 단축.","공격력 2배 범위 피해 + 3초 동안 50% 감속.","범위 내 적 1초 정지 + 피해 5 보호막.","공격력 4배의 넓은 범위 폭발.","공격력 1.5배 포탄을 한 개씩 세 번 발사.","공격력 2배 범위 피해 + 6초 동안 껍질 방어 제거.","5초 동안 범위 내 적이 받는 피해 25% 증가.","4초 동안 전체 기본 공격 추가 발사 및 간격 15% 단축.","자동 조준은 체력이 가장 높은 적. 한 적에게 공격력 5배 피해, 방어 무시.","공격력 1배 관통탄을 다섯 번 발사. 각 탄 최대 열 적 관통.","3초 동안 적을 끌어당기고 총 공격력 2배 지속 피해.","공격력 5배의 넓은 범위 폭발.","방어선 체력 5 회복. 최대 체력은 100.","5초 동안 포자 범위에 초당 공격력 80% 피해.","공격력 2배 범위 피해 + 4초 동안 40% 감속.","공격력 6배 직선 탄환. 껍질 방어 무시.","공격력 2배 범위 피해 + 1.5초 정지.","공격력 80% 탄환을 한 개씩 열 번 발사.","공격력 1.2배 당근을 한 개씩 다섯 번 빠르게 발사.","공격력 3배 범위 피해 + 2초 동안 30% 감속, 방어 무시.","8초 안에 다음 여섯 번의 기본 공격 피해 두 배.","공격력 80%의 작은 폭발을 표식 주변에 일곱 번 발생.","포물선 폭탄: 공격력 4배 폭발 + 3초 동안 30% 감속."};
        public static string SkillDetail(int id){return (id<0?"흙장난 범위 안의 적을 3초 동안 30% 느리게 해요.":skillDetails[id])+(new[]{1,3,18,21}.Contains(id)?" 장판 반경은 기존의 2배.":"");}
        public static bool HasDamagingSkill(int id){return id>=0&&!new[]{0,1,4,5,6,7,8,10,14,15,20}.Contains(id);}
    }
    public class Defender {
        public Pet Pet;public DefenseStats Stats;public float X,Y;public double ShotClock,SkillClock,Motion=10,BubbleClock;public string BubbleLine="";
        public double PowerTime,SpeedTime,DoubleTime,SelfTime,EmpowerTime;public int EmpowerShots,Shots,LastTarget=-1,Consecutive,Slot;
        public double DamageBonus=1,SpeedBonus=1,SkillBonus=1,CooldownBonus=1;
        public double AttackPower {get{return Stats.Damage*DamageBonus;}}
        public double AttackInterval {get{return Math.Max(.15,Stats.Interval/SpeedBonus);}}
        public double SkillPower {get{return AttackPower*SkillBonus;}}
        public double SkillCooldown {get{return Math.Max(5,Stats.Cooldown*CooldownBonus);}}
        public PointF Point {get{return new PointF(X,Y);}}
        public Defender(Pet pet,int slot){Pet=new Pet {Id=pet.Id,Name=pet.Name,SpeciesId=pet.SpeciesId,EvolutionId=pet.EvolutionId};Stats=DefenseStats.For(Pet);Slot=slot;X=920;Y=210+slot*62;}
    }
    public class DefenseEnemy {
        public int Id,Type;public float X,Y;public double HP,MaxHP,Age,SlowTime,Slow=.0,StopTime,StopImmune,WeakTime,Weak=1,ArmorTime,PoisonTime,PoisonRate;
        public bool Dead;public float Radius {get{return (Type==5?38:Type==4?12:24)*.7f;}}
        public double HopClock;
        public const double HopDuration=1,HopRest=1.2;
        public int Frame {get{return Type==1?(HopClock%(HopDuration+HopRest)<HopDuration?Math.Min(7,(int)(HopClock%(HopDuration+HopRest)/HopDuration*8)):0):(int)(Age/.15)%8;}}
        public float DrawY {get {if(Type!=1)return Y;double phase=HopClock%(HopDuration+HopRest);return Y-(phase<HopDuration?(float)(45*Math.Sin(Math.PI*phase/HopDuration)):0);}}
        public void Move(double dt,double speed){
            if(Type!=1){X+=(float)(speed*dt);return;}
            // Count airborne time across boundaries without moving during rest.
            double cycle=HopDuration+HopRest,before=HopClock,after=before+dt;
            double airborneBefore=Math.Floor(before/cycle)*HopDuration+Math.Min(before%cycle,HopDuration);
            double airborneAfter=Math.Floor(after/cycle)*HopDuration+Math.Min(after%cycle,HopDuration);
            X+=(float)(speed*(airborneAfter-airborneBefore));HopClock=after;
        }
        public PointF Point {get{return new PointF(X,DrawY);}}
    }
    public class DefenseShot {
        public Defender Owner;public PointF Start,Target,Point;public double Age,Duration,Damage,Distance;public string Trajectory;public bool Skill;public int Pierce;
        public HashSet<int> Hit=new HashSet<int>();
    }
    public class DefenseEffect {public int EvolutionId=-1;public int Species;public bool Skill;public PointF Point;public double Age,Life=.3,Radius;public bool Ground;public Defender Follow;}
    public class DefenseBeam {public Defender Owner;public PointF Start,Target;public double Age,Duration=1,Rate;}
    public class DefenseZone {public Defender Owner;public PointF Point;public double Left,Rate,Slow,Radius=170,Pull;}
    public class DefenseVolley {public Defender Owner;public PointF Target;public double Left,Interval,Damage;public int Count,Pierce;public string Trajectory;}

    public class DefenseBattle {
        public readonly List<Defender> Team=new List<Defender>();public readonly List<DefenseEnemy> Enemies=new List<DefenseEnemy>();
        public readonly List<DefenseBeam> Beams=new List<DefenseBeam>();
        public readonly List<DefenseShot> Shots=new List<DefenseShot>();public readonly List<DefenseEffect> Effects=new List<DefenseEffect>();
        public readonly List<DefenseZone> Zones=new List<DefenseZone>();public readonly List<DefenseVolley> Volleys=new List<DefenseVolley>();
        public SaveData Data;public Action Persist;public PointF? Marker;public int Wave,Completed,Kills,Rewards,Remaining;public double House=100,Shield,ShieldTime,Between=1,SpawnClock,Time;
        public bool Running=true;Random random;int nextId;public string Notice="준비!";
        public readonly List<DefenseCard> Cards=new List<DefenseCard>();public readonly List<string> Upgrades=new List<string>();
        public bool ChoosingCard {get{return Cards.Count>0;}}
        public float HitScaleX=1,HitScaleY=1;
        public double HitDistance(PointF a,PointF b){return Distance(new PointF(a.X*HitScaleX,a.Y*HitScaleY),new PointF(b.X*HitScaleX,b.Y*HitScaleY));}
        public double HitSegmentDistance(PointF a,PointF b,PointF p){return SegmentDistance(new PointF(a.X*HitScaleX,a.Y*HitScaleY),new PointF(b.X*HitScaleX,b.Y*HitScaleY),new PointF(p.X*HitScaleX,p.Y*HitScaleY));}
        public void ToggleMarker(PointF point){Marker=Marker.HasValue?(PointF?)null:point;}
        void OfferCards(){Cards.Clear();var attackers=Team.Where(d=>DefenseStats.HasDamagingSkill(d.Pet.SpeciesId)).ToArray();var kinds=Enumerable.Range(0,8).Where(i=>i!=4&&i!=5||attackers.Length>0).OrderBy(i=>random.Next()).Take(3);foreach(int kind in kinds){Defender target=null;if(kind%2==0)target=kind==4?attackers[random.Next(attackers.Length)]:Team[random.Next(Team.Count)];Cards.Add(new DefenseCard(kind,target));}Notice="라운드 완료 · 강화 카드 1장을 선택해 주세요.";}
        public bool ChooseCard(int index){if(!Running||index<0||index>=Cards.Count)return false;var card=Cards[index];foreach(var d in Team.Where(d=>card.Target==null||d==card.Target)){switch(card.Kind/2){case 0:d.DamageBonus*=card.Target==null?1.12:1.3;break;case 1:d.SpeedBonus=Math.Min(4,d.SpeedBonus*(card.Target==null?1.1:1.25));break;case 2:d.SkillBonus*=card.Target==null?1.15:1.35;break;case 3:d.CooldownBonus=Math.Max(5/d.Stats.Cooldown,d.CooldownBonus*(card.Target==null?.9:.8));break;}}Upgrades.Add(card.Title+" · "+card.Scope);Cards.Clear();Between=3;Notice="강화 완료! 다음 라운드까지";return true;}
        public DefenseBattle(SaveData data,IEnumerable<Pet> pets,int seed=0){Data=data;random=seed==0?new Random():new Random(seed);foreach(Pet p in pets.Where(p=>!p.Dead).Take(5))Team.Add(new Defender(p,Team.Count));if(Team.Count==0)throw new ArgumentException("고구마를 한 마리 이상 선택해 주세요.");}
        public static double Distance(PointF a,PointF b){double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
        public bool InRange(Defender d,PointF target){return true;}
        public DefenseEnemy Closest(Defender d){return Enemies.Where(e=>!e.Dead&&InRange(d,e.Point)).OrderByDescending(e=>e.X).FirstOrDefault();}
        public DefenseEnemy Spawn(int type,float x=0,float y=350){double factor=1+.18*Math.Max(0,Wave-1);double[] hp={42,30,95,80,15,420};var e=new DefenseEnemy {Id=++nextId,Type=type,X=x,Y=y,HP=hp[type]*factor,MaxHP=hp[type]*factor};Enemies.Add(e);return e;}
        public void StartWave(){Wave++;Remaining=Math.Min(50,10+Wave*2);SpawnClock=0;Notice="웨이브 "+Wave;}
        void Save(){if(Persist!=null)Persist();}
        public void Finish(){if(!Running)return;Running=false;Beams.Clear();if(Completed>Data.DefenseBestWave||Completed==Data.DefenseBestWave&&Kills>Data.DefenseBestKills){Data.DefenseBestWave=Completed;Data.DefenseBestKills=Kills;}Save();Notice="종료 · 완료 "+Completed+"웨이브 / "+Kills+"처치 / 씨앗 +"+Rewards;}
        public void Kill(DefenseEnemy e){if(e.Dead)return;e.Dead=true;Kills++;if(Kills%10==0){Data.Seeds++;Rewards++;Save();}if(e.Type==3){for(int i=0;i<3;i++)Spawn(4,Math.Max(0,e.X-12-i*8),Math.Max(120,Math.Min(520,e.Y+(i-1)*24)));}}
        public void Damage(DefenseEnemy e,double amount,double pierce=0){if(e.Dead||amount<=0)return;double armor=e.Type==2&&e.ArmorTime<=0?.45:0;e.HP-=amount*(1-armor*(1-pierce))*(e.WeakTime>0?e.Weak:1);if(e.HP<=0)Kill(e);}
        public void SlowEnemy(DefenseEnemy e,double strength,double duration){if(e.Type==5){strength*=.5;duration*=.5;}if(e.SlowTime<=0||strength>=e.Slow){e.Slow=strength;e.SlowTime=Math.Max(e.SlowTime,duration);}}
        public void StopEnemy(DefenseEnemy e,double duration){if(e.StopTime>0||e.StopImmune>0)return;e.StopTime=e.Type==5?duration*.5:duration;}
        public void Area(PointF target,double radius,double damage,double slow=0,double seconds=0,double stop=0,double pierce=0){foreach(var e in Enemies.ToArray())if(!e.Dead&&HitDistance(e.Point,target)<=radius*.7+e.Radius){Damage(e,damage,pierce);if(!e.Dead){if(slow>0)SlowEnemy(e,slow,seconds);if(stop>0)StopEnemy(e,stop);}}}
        void Effect(Defender d,PointF target,bool skill,double life=.3,bool ground=false,Defender follow=null,double radius=0){Effects.Add(new DefenseEffect {Species=d.Pet.SpeciesId,EvolutionId=d.Pet.EvolutionId,Point=target,Skill=skill,Life=life,Ground=ground,Follow=follow,Radius=radius});}
        void Launch(Defender d,PointF target,double damage,bool skill,string trajectory,int pierce=1){d.Motion=0;double distance=Distance(d.Point,target);
            if(!skill&&trajectory=="beam"){
                if(!Beams.Any(b=>b.Owner==d)){
                    PointF direction=distance<1?new PointF(-1,0):new PointF((target.X-d.X)/(float)distance,(target.Y-d.Y)/(float)distance);
                    Beams.Add(new DefenseBeam {Owner=d,Start=d.Point,Target=new PointF(d.X+direction.X*1200,d.Y+direction.Y*1200),Rate=damage*2});
                }
                return;
            }if(trajectory=="point"){Area(target,skill?85:40,damage);Effect(d,target,skill);return;}Shots.Add(new DefenseShot {Owner=d,Start=d.Point,Target=target,Point=d.Point,Damage=damage,Skill=skill,Trajectory=trajectory,Pierce=pierce,Distance=distance,Duration=trajectory=="parabolic"?Math.Min(1.25,.35+distance/850):Math.Max(.1,1300/900.0)});}
        void OnBasicHit(DefenseShot s,DefenseEnemy e){int id=s.Owner.Pet.SpeciesId;double damage=s.Damage;if(id==0){s.Owner.Consecutive=s.Owner.LastTarget==e.Id?Math.Min(4,s.Owner.Consecutive+1):0;s.Owner.LastTarget=e.Id;damage*=1+s.Owner.Consecutive*.05;}if(id==16&&e.Type==5)damage*=1.2;Damage(e,damage,new[]{2,13,27}.Contains(id)?.5:0);if(id==1)SlowEnemy(e,.15,1);if(id==9)SlowEnemy(e,.2,1);if(id==14){e.Weak=e.WeakTime>0?Math.Max(e.Weak,1.1):1.1;e.WeakTime=Math.Max(2,e.WeakTime);}if((id==21||id==5&&s.Owner.Pet.EvolutionId==8)&&!e.Dead){e.PoisonTime=3;e.PoisonRate=Math.Max(e.PoisonRate,4*s.Owner.DamageBonus);}if(id==22){var next=Enemies.Where(n=>!n.Dead&&n!=e&&HitDistance(n.Point,e.Point)<180*.7).OrderBy(n=>Distance(n.Point,e.Point)).FirstOrDefault();if(next!=null){Damage(next,damage*.5);Effect(s.Owner,next.Point,false);}}if(id==3)foreach(var next in Enemies.ToArray())if(next!=e&&!next.Dead&&HitDistance(next.Point,e.Point)<45*.7)Damage(next,damage*.5);}
        void Impact(DefenseShot shot,PointF p){int id=shot.Owner.Pet.SpeciesId;double radius=shot.Skill?(id==30?140:id==12?130:100):100;if(shot.Skill){if(id==30)Area(p,radius,shot.Damage,.3,3);else Area(p,radius,shot.Damage,0,0,0,id==2||id==23?1:0);}else if(shot.Trajectory=="parabolic"){foreach(var e in Enemies.ToArray())if(!e.Dead&&HitDistance(e.Point,p)<radius*.7+e.Radius)OnBasicHit(shot,e);}Effect(shot.Owner,p,shot.Skill,radius:radius);}
        void Volley(Defender d,PointF target,int count,double multiplier,double interval,string trajectory,int pierce=1){Volleys.Add(new DefenseVolley {Owner=d,Target=target,Count=count,Damage=d.SkillPower*multiplier,Interval=interval,Trajectory=trajectory,Pierce=pierce});d.Motion=0;}
        void Zone(Defender d,PointF target,double life,double rate,double slow=0,double pull=0){var zone=new DefenseZone {Owner=d,Point=target,Left=life,Rate=rate,Slow=slow,Pull=pull};Zones.Add(zone);Effect(d,target,true,life,true,radius:zone.Radius);}
        public bool Skill(Defender d){if(!Running||ChoosingCard||Between>0||d.SkillClock>0)return false;var foe=Closest(d);PointF target=Marker??(foe!=null?foe.Point:new PointF(Math.Max(0,d.X-250),d.Y));int id=d.Pet.SpeciesId;bool support=new[]{0,4,5,7,8,15,20,28}.Contains(id);if(!support&&!InRange(d,target))return false;d.SkillClock=d.SkillCooldown;d.BubbleLine=Dialogue.Skill(d.Pet,d.Stats.Line);d.BubbleClock=2;d.Motion=0;double a=d.SkillPower;
            switch(id){
                case 0:d.SelfTime=8;Effect(d,d.Point,true,8,false,d);break;
                case 1:Zone(d,target,5,0,.4);break;
                case 2:Volley(d,target,1,4,0,"straight",3);break;
                case 3:Area(target,80,3*a);Zone(d,target,4,a*.5);break;
                case 4:d.SelfTime=5;Effect(d,d.Point,true,5,false,d);break;
                case 5:foreach(var f in Team){f.PowerTime=6;Effect(d,f.Point,true,6,false,f);}break;
                case 6:Area(target,85,0,.4,3,1);Effect(d,target,true,4,true);break;
                case 7:Shield=10;ShieldTime=8;Effect(d,new PointF(950,340),true,8);break;
                case 8:foreach(var f in Team){f.SpeedTime=6;Effect(d,f.Point,true,6,false,f);}break;
                case 9:Area(target,85,2*a,.5,3);Effect(d,target,true);break;
                case 10:Area(target,85,0,0,0,1);Shield=Math.Max(5,Shield);ShieldTime=Math.Max(8,ShieldTime);Effect(d,target,true);break;
                case 11:Area(target,100,4*a);Effect(d,target,true);break;
                case 12:Volley(d,target,3,1.5,.22,"parabolic");break;
                case 13:foreach(var e in Enemies.ToArray())if(!e.Dead&&HitDistance(e.Point,target)<85*.7+e.Radius){e.ArmorTime=6;Damage(e,2*a);}Effect(d,target,true);break;
                case 14:foreach(var e in Enemies)if(!e.Dead&&HitDistance(e.Point,target)<85*.7+e.Radius){e.Weak=1.25;e.WeakTime=5;}Effect(d,target,true,5,true);break;
                case 15:foreach(var f in Team){f.DoubleTime=4;Effect(d,f.Point,true,4,false,f);}break;
                case 16:var big=Enemies.Where(e=>!e.Dead&&InRange(d,e.Point)).OrderByDescending(e=>e.HP).FirstOrDefault();if(!Marker.HasValue&&big!=null)target=big.Point;var marked=Enemies.Where(e=>!e.Dead&&HitDistance(e.Point,target)<35*.7+e.Radius).OrderBy(e=>Distance(e.Point,target)).FirstOrDefault();if(marked!=null)Damage(marked,5*a,1);Effect(d,target,true);break;
                case 17:Volley(d,target,5,1,.12,"straight",10);break;
                case 18:Zone(d,target,3,a*2/3,0,70);break;
                case 19:Area(target,100,5*a);Effect(d,target,true);break;
                case 20:House=Math.Min(100,House+5);Effect(d,new PointF(950,340),true);break;
                case 21:Zone(d,target,5,a*.8);break;
                case 22:Area(target,85,2*a,.4,4);Effect(d,target,true);break;
                case 23:Volley(d,target,1,6,0,"straight");break;
                case 24:Area(target,85,2*a,0,0,1.5);Effect(d,target,true);break;
                case 25:Volley(d,target,10,.8,.07,"straight");break;
                case 26:Volley(d,target,5,1.2,.16,"straight");break;
                case 27:Area(target,85,3*a,.3,2,0,1);Effect(d,target,true);break;
                case 28:d.EmpowerTime=8;d.EmpowerShots=6;Effect(d,d.Point,true,8,false,d);break;
                case 29:for(int i=0;i<7;i++){PointF q=new PointF(target.X+random.Next(-40,41),target.Y+random.Next(-30,31));Area(q,40,.8*a);}Effect(d,target,true);break;
                case 30:Volley(d,target,1,4,0,"parabolic");break;
                default:Area(target,80,0,.3,3);Effect(d,target,true,3,true);break;
            }return true;
        }
        public void Step(double dt){if(!Running||ChoosingCard||dt<=0)return;dt=Math.Min(.1,dt);Time+=dt;ShieldTime=Math.Max(0,ShieldTime-dt);if(ShieldTime==0)Shield=0;
            foreach(var d in Team){d.Motion+=dt;d.BubbleClock=Math.Max(0,d.BubbleClock-dt);d.SkillClock=Math.Max(0,d.SkillClock-dt);d.PowerTime=Math.Max(0,d.PowerTime-dt);d.SpeedTime=Math.Max(0,d.SpeedTime-dt);d.DoubleTime=Math.Max(0,d.DoubleTime-dt);d.SelfTime=Math.Max(0,d.SelfTime-dt);d.EmpowerTime=Math.Max(0,d.EmpowerTime-dt);if(d.EmpowerTime==0)d.EmpowerShots=0;}
            if(Between>0){Between-=dt;if(Between<=0)StartWave();return;}
            SpawnClock-=dt;if(Remaining>0&&SpawnClock<=0){int type=0;if(Wave%5==0&&Remaining==1)type=5;else {int r=random.Next(100);if(Wave>=10&&r<18)type=3;else if(Wave>=4&&r<38)type=2;else if(Wave>=2&&r<60)type=1;else if(Wave>=6&&r<70)type=4;}Spawn(type,0,Math.Max(120,Math.Min(520,Team.Count==0?460:Team[random.Next(Team.Count)].Y+random.Next(-15,16))));Remaining--;SpawnClock=Math.Max(.35,1.2-Wave*.025);}
            foreach(var zone in Zones.ToArray()){zone.Left-=dt;foreach(var e in Enemies.ToArray())if(!e.Dead&&HitDistance(e.Point,zone.Point)<zone.Radius*.7+e.Radius){if(zone.Rate>0)Damage(e,zone.Rate*dt);if(zone.Slow>0)SlowEnemy(e,zone.Slow,.2);if(zone.Pull>0){float factor=(float)(zone.Pull*dt*(e.Type==5?.5:1));e.X+=(zone.Point.X-e.X)*(float)Math.Min(.2,factor/100);e.Y+=(zone.Point.Y-e.Y)*(float)Math.Min(.2,factor/100);}}if(zone.Left<=0)Zones.Remove(zone);}
            foreach(var e in Enemies.ToArray()){if(e.Dead)continue;e.Age+=dt;e.SlowTime=Math.Max(0,e.SlowTime-dt);e.StopImmune=Math.Max(0,e.StopImmune-dt);e.WeakTime=Math.Max(0,e.WeakTime-dt);e.ArmorTime=Math.Max(0,e.ArmorTime-dt);if(e.PoisonTime>0){double poisoned=Math.Min(dt,e.PoisonTime);e.PoisonTime=Math.Max(0,e.PoisonTime-dt);Damage(e,e.PoisonRate*poisoned);}if(e.Dead)continue;if(e.StopTime>0){e.StopTime=Math.Max(0,e.StopTime-dt);if(e.StopTime==0)e.StopImmune=3;}else {double[] speed={24,44,16,20,36,12};e.Move(dt*(e.SlowTime>0?1-e.Slow:1),Math.Min(85,speed[e.Type]+Wave*.8));}if(e.X>=950){e.Dead=true;double[] leaks={5,4,8,10,2,20};double hit=leaks[e.Type],absorbed=Math.Min(Shield,hit);Shield-=absorbed;House=Math.Max(0,House-hit+absorbed);if(House==0){Finish();return;}}}
            foreach(var beam in Beams.ToArray()){
                double active=Math.Min(dt,Math.Max(0,beam.Duration-beam.Age));
                foreach(var enemy in Enemies.ToArray())if(!enemy.Dead&&HitSegmentDistance(beam.Start,beam.Target,enemy.Point)<enemy.Radius+7*.7)Damage(enemy,beam.Rate*active);
                beam.Age+=active;if(beam.Age>=beam.Duration-1e-9)Beams.Remove(beam);
            }
            foreach(var d in Team){d.ShotClock-=dt;if(d.ShotClock>0)continue;var e=Closest(d);PointF? target=Marker??(e==null?(PointF?)null:e.Point);if(!target.HasValue||!InRange(d,target.Value))continue;double power=d.AttackPower*(d.PowerTime>0?1.2:1);d.Shots++;if(d.DoubleTime>0&&d.Stats.Trajectory=="beam")power*=2;if(d.Pet.SpeciesId==28&&d.Shots%4==0)power*=2;if(d.EmpowerShots>0){power*=2;d.EmpowerShots--;}Launch(d,target.Value,power,false,d.Stats.Trajectory,d.Pet.SpeciesId==17?2:1);if(d.DoubleTime>0)Launch(d,target.Value,power,false,d.Stats.Trajectory);double factor=d.DoubleTime>0?.85:1;if(d.SpeedTime>0)factor=Math.Min(factor,.75);if(d.SelfTime>0)factor=Math.Min(factor,d.Pet.SpeciesId==4?.5:.7);d.ShotClock=Math.Max(d.Stats.Trajectory=="beam"?1.1:.1,d.AttackInterval*factor);}
            foreach(var v in Volleys.ToArray()){v.Left-=dt;if(v.Left<=0){Launch(v.Owner,v.Target,v.Damage,true,v.Trajectory,v.Pierce);v.Count--;v.Left+=v.Interval;if(v.Count<=0)Volleys.Remove(v);}}
            foreach(var s in Shots.ToArray()){s.Age+=dt;PointF before=s.Point;double u=s.Age/s.Duration;if(s.Trajectory=="parabolic"){u=Math.Min(1,u);s.Point=new PointF((float)(s.Start.X+(s.Target.X-s.Start.X)*u),(float)(s.Start.Y+(s.Target.Y-s.Start.Y)*u-100*4*u*(1-u)));if(u>=1){Impact(s,s.Target);Shots.Remove(s);}}else {double distance=Math.Max(1,s.Distance);s.Point=new PointF((float)(s.Start.X+(s.Target.X-s.Start.X)/distance*900*s.Age),(float)(s.Start.Y+(s.Target.Y-s.Start.Y)/distance*900*s.Age));foreach(var e in Enemies.ToArray())if(!e.Dead&&!s.Hit.Contains(e.Id)&&HitSegmentDistance(before,s.Point,e.Point)<e.Radius+7*.7){s.Hit.Add(e.Id);if(s.Skill){Damage(e,s.Damage,s.Owner.Pet.SpeciesId==2||s.Owner.Pet.SpeciesId==23?1:0);Effect(s.Owner,e.Point,true);}else {OnBasicHit(s,e);Effect(s.Owner,e.Point,false);}if(s.Hit.Count>=s.Pierce){Shots.Remove(s);break;}}if(s.Age>=s.Duration||s.Point.X<0||s.Point.X>1000||s.Point.Y<100||s.Point.Y>540)Shots.Remove(s);}}
            foreach(var fx in Effects.ToArray()){fx.Age+=dt;if(fx.Follow!=null)fx.Point=fx.Follow.Point;if(fx.Age>=fx.Life)Effects.Remove(fx);}
            Enemies.RemoveAll(e=>e.Dead);if(Remaining==0&&Enemies.Count==0){Completed=Wave;House=Math.Min(100,House+5);Between=5;Shots.Clear();Beams.Clear();Volleys.Clear();Zones.Clear();Effects.Clear();OfferCards();if(Completed>Data.DefenseBestWave||Completed==Data.DefenseBestWave&&Kills>Data.DefenseBestKills){Data.DefenseBestWave=Completed;Data.DefenseBestKills=Kills;}Save();}
        }
        public static double SegmentDistance(PointF a,PointF b,PointF p){double x=b.X-a.X,y=b.Y-a.Y,length=x*x+y*y;if(length==0)return Distance(a,p);double t=Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/length));return Distance(new PointF((float)(a.X+t*x),(float)(a.Y+t*y)),p);}
    }

    public class DefenseArt:IDisposable {
        Dictionary<string,Bitmap[]> cache=new Dictionary<string,Bitmap[]>();public List<string> Missing=new List<string>();
        Bitmap intro;public Bitmap Intro {get {if(intro==null){string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","defense","고구마버러지디펜스Intro.png");if(File.Exists(path))using(var source=new Bitmap(path))intro=new Bitmap(source);}return intro;}}
        public Bitmap Frame(string folder,int id,int frame){string key=folder+":"+id;Bitmap[] images;if(!cache.TryGetValue(key,out images)){int count=folder=="enemies"?8:4;images=new Bitmap[count];for(int i=0;i<count;i++){string path=Path.Combine(Paths.BaseDirectory,"assets","higgsfield","defense",folder,id.ToString("00"),i.ToString("00")+".png");if(File.Exists(path)){using(var source=new Bitmap(path))images[i]=new Bitmap(source);}else Missing.Add(path);}cache[key]=images;}return images[Math.Max(0,Math.Min(images.Length-1,frame))];}
        public void Draw(Graphics g,string folder,int id,int frame,PointF point,int size){Bitmap image=Frame(folder,id,frame);if(image!=null)g.DrawImage(image,new RectangleF(point.X-size/2,point.Y-size/2,size,size));else {using(var brush=new SolidBrush(folder=="enemies"?Color.ForestGreen:Color.Gold))g.FillEllipse(brush,point.X-8,point.Y-8,16,16);}}
        public void Dispose(){foreach(var images in cache.Values)foreach(var image in images)if(image!=null)image.Dispose();cache.Clear();if(intro!=null){intro.Dispose();intro=null;}}
    }
}
