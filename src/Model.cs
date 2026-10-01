using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace GuMaGoChi {
    public static class Paths {
        public static readonly string BaseDirectory=Path.GetDirectoryName(typeof(Paths).Assembly.Location);
    }
    public class Species {
        public int Id; public string Name, Personality, Skill;
        public Species(int id, string name, string personality, string skill) { Id=id; Name=name; Personality=personality; Skill=skill; }
    }
    public static class Catalog {
        public static readonly Species[] All = {
            new Species(0,"밤톨이","소심하지만 성실함","집중"),new Species(1,"꿀떡이","느긋하고 애교 많음","친화"),
            new Species(2,"자몽이","새침한 츤데레","정밀"),new Species(3,"군밤이","차분하고 다정함","힘"),
            new Species(4,"새싹이","호기심 많은 낙천가","민첩"),new Species(5,"딸기마","관심받고 싶은 새침쟁이","친화"),
            new Species(6,"구름이","몽상가인 게으름뱅이","집중"),new Species(7,"달콩이","조용하고 다정함","친화"),
            new Species(8,"햇살이","열정적인 응원대장","힘"),new Species(9,"눈송이","다혈질 눈송이","정밀"),
            new Species(10,"왕고마","위엄 뒤에 숨은 겁쟁이","힘"),new Species(11,"마법마","시니컬한 호기심쟁이","집중"),
            new Species(12,"해적마","깔끔한 모험가","힘"),new Species(13,"요리마","다정한 잔소리꾼","정밀"),
            new Species(14,"화가마","예민하고 감성적임","정밀"),new Species(15,"음표마","흥 많고 산만함","친화"),
            new Species(16,"책벌레마","깐깐한 아는 척쟁이","집중"),new Species(17,"탐험마","겁 없는 모험가","민첩"),
            new Species(18,"우주마","엉뚱하지만 침착함","집중"),new Species(19,"별똥이","승부욕 강한 허세쟁이","힘"),
            new Species(20,"꽃송이","다정한 질투쟁이","친화"),new Species(21,"버섯마","낯가리는 자기 페이스","집중"),
            new Species(22,"개굴마","장난꾸러기","민첩"),new Species(23,"냥고마","도도한 츤데레","정밀"),
            new Species(24,"멍고마","발끈하는 치와와형","친화"),new Species(25,"토끼마","겁 많지만 빠름","민첩"),
            new Species(26,"당근마","고집 센 노력파","힘"),new Species(27,"민트마","직설적인 시니컬형","정밀"),
            new Species(28,"초코마","느긋한 승부사","민첩"),new Species(29,"무지개마","표현이 큰 변덕쟁이","민첩"),new Species(30,"간첩마","능청스러운 감자 위장꾼","집중")
        };
    }
    public class Pet {
        public string Id=Guid.NewGuid().ToString(), Name="고구마", LastWords="", DiedAt="", Cause="";
        public int SpeciesId=-1, PendingSpecies=-1, Waste=0, X=0, Y=0;
        public bool Active=true, Home=false, Sleeping=false, GrowthReady=false, Dead=false;
        public double Age=0, Hunger=15, Dirt=5, Fatigue=5, Illness=0;
        public double FoodCooldown=0, MedicineCooldown=0, PetCooldown=0, PlayCooldown=0, TrainCooldown=0;
        public double WasteClock=0, SleepClock=0, TalkClock=0;
        public int Affection=0, Play=0, Training=0, Shots=0, Goals=0, Care=0;
        public List<int> GrowthHistory=new List<int>();
        public Dictionary<string,int> Friends=new Dictionary<string,int>();
        public string Kind { get { return SpeciesId<0 ? "아기 고구마" : Catalog.All[SpeciesId].Name; } }
        public string Skill { get { return SpeciesId<0 ? "성장 중" : Catalog.All[SpeciesId].Skill; } }
        public string Personality { get { return SpeciesId<0 ? "호기심 많은 아기" : Catalog.All[SpeciesId].Personality; } }
    }
    public class SaveData {
        public int Version=1, Seeds=0, Dew=0, Medicine=0;
        public List<Pet> Pets=new List<Pet>();
        public List<int> Discovered=new List<int>();
        public bool AutoStart=false;
    }
    public class Engine {
        public const double AdultAge=8*3600, Life=100*3600;
        public const int AdoptPrice=100, DewPrice=8, MedicinePrice=12;
        public SaveData Data; public Random Random;
        public Engine(SaveData data, int? seed=null) { Data=data; Random=seed.HasValue ? new Random(seed.Value) : new Random(); }
        public static double Clamp(double value) { return Math.Max(0,Math.Min(100,value)); }
        public Pet Adopt(string name, bool first=false) {
            bool rescue=!Data.Pets.Any(p=>!p.Dead) && Data.Seeds<AdoptPrice;
            if (!first && !rescue && Data.Seeds<AdoptPrice) return null;
            if (!first && !rescue) Data.Seeds-=AdoptPrice;
            Pet pet=new Pet { Name=name.Trim(), TalkClock=Random.Next(900,1801) }; Data.Pets.Add(pet); return pet;
        }
        public void Tick(double seconds) {
            foreach(Pet p in Data.Pets) {
                if(!p.Active || p.Dead || p.GrowthReady) continue;
                // Stop exactly at the first age boundary; ready pets never accumulate hidden time.
                double boundary=p.SpeciesId<0 ? AdultAge : Life;
                double dt=Math.Min(seconds,Math.Max(0,boundary-p.Age));
                p.Age+=dt;
                p.FoodCooldown=Math.Max(0,p.FoodCooldown-dt); p.MedicineCooldown=Math.Max(0,p.MedicineCooldown-dt);
                p.PetCooldown=Math.Max(0,p.PetCooldown-dt);p.PlayCooldown=Math.Max(0,p.PlayCooldown-dt);p.TrainCooldown=Math.Max(0,p.TrainCooldown-dt);
                p.TalkClock-=dt;
                double hours=dt/3600;
                p.Hunger=Clamp(p.Hunger+hours*(p.Sleeping?3:8));
                p.Dirt=Clamp(p.Dirt+hours*(p.Sleeping?1:3)+hours*p.Waste*2);
                p.Fatigue=Clamp(p.Fatigue+hours*(p.Sleeping?-35:10));
                if(p.Hunger>=80 || p.Dirt>=70 || p.Fatigue>=95) p.Illness=Clamp(p.Illness+hours*18);
                else if(p.Sleeping) p.Illness=Math.Max(p.Illness>0?1:0,p.Illness-hours*3);
                p.WasteClock+=dt;
                if(p.WasteClock>=5400) { p.WasteClock-=5400;p.Waste=Math.Min(6,p.Waste+1);p.Dirt=Clamp(p.Dirt+8); }
                if(p.Sleeping) {p.SleepClock+=dt;if(p.Fatigue<=5 && p.SleepClock>=600) {p.Sleeping=false;p.SleepClock=0;}}
                if(p.Illness>=100) { Kill(p,"질병"); continue; }
                if(p.Age>=Life) { Kill(p,"자연사");continue; }
                if(p.SpeciesId<0 && p.Age>=AdultAge) { p.PendingSpecies=ChooseSpecies(p);p.GrowthReady=true;p.Sleeping=false; }
            }
        }
        public List<Species> Candidates(Pet p) { return Catalog.All.Where(s=>!(p.Training>=12 && s.Id==6)).ToList(); }
        public int ChooseSpecies(Pet p) {
            var candidates=Candidates(p); var weights=candidates.Select(s=> {
                if(s.Skill=="친화") return 1+p.Affection*.8;
                if(s.Skill=="민첩") return 1+p.Play*.8;
                if(s.Skill=="힘") return 1+p.Play*.4+p.Training*.4;
                return 1+p.Training*.8;
            }).ToArray();
            double roll=Random.NextDouble()*weights.Sum();for(int i=0;i<candidates.Count;i++) {roll-=weights[i];if(roll<=0)return candidates[i].Id;}
            return candidates.Last().Id;
        }
        public bool Reveal(Pet p) {
            if(!p.GrowthReady || p.PendingSpecies<0 || p.Dead)return false;
            p.SpeciesId=p.PendingSpecies;p.PendingSpecies=-1;p.GrowthReady=false;p.GrowthHistory.Add(p.SpeciesId);
            bool fresh=!Data.Discovered.Contains(p.SpeciesId);if(fresh)Data.Discovered.Add(p.SpeciesId);
            Data.Seeds+=fresh?30:15;return true;
        }
        public bool CanCare(Pet p) { return p.Active && !p.Dead && !p.GrowthReady && !p.Sleeping; }
        public string Feed(Pet p, bool dew) {
            if(!CanCare(p))return "지금은 먹을 수 없어요.";
            if(p.FoodCooldown>0)return "먹이 쿨타임이 남아 있어요.";
            if(p.Hunger<=0)return "이미 배가 불러요.";
            if(dew && Data.Dew<=0)return "아침 이슬이 없어요. 상점에서 구매해 주세요.";
            double before=p.Hunger;p.Hunger=Clamp(p.Hunger-(dew?30:10));p.FoodCooldown=300;if(dew)Data.Dew--;
            // A tiny restored deficit cannot be farmed for full care rewards.
            if(before-p.Hunger>=5) {Data.Seeds+=10;p.Care++;}
            return Dialogue.Get(p,0);
        }
        public string Treat(Pet p, bool strong) {
            if(!CanCare(p))return "지금은 치료할 수 없어요.";
            if(p.MedicineCooldown>0)return "치료 쿨타임이 남아 있어요.";
            if(p.Illness<=0)return "건강해요!";
            if(strong && Data.Medicine<=0)return "상위 치료가 없어요.";
            p.Illness=Clamp(p.Illness-(strong?30:10));p.MedicineCooldown=600;if(strong)Data.Medicine--;
            return Dialogue.Get(p,1);
        }
        public string Stroke(Pet p) {
            if(!CanCare(p))return "지금은 쉬고 있어요.";
            if(p.PetCooldown<=0) {p.Affection++;p.PetCooldown=900;Data.Seeds+=2;return Dialogue.Get(p,2);}
            return Dialogue.Get(p,2);
        }
        public bool Clean(Pet p) {
            if(!p.Active || p.Dead || p.GrowthReady || p.Waste<=0)return false;
            p.Waste--;p.Dirt=Clamp(p.Dirt-20);p.Care++;Data.Seeds+=8;return true;
        }
        public void FinishActivity(Pet p, bool train, int goals, int shots) {
            if(!CanCare(p))return;
            if(train) {p.Shots+=shots;p.Goals+=goals;if(p.TrainCooldown<=0) {p.Training++;p.Affection++;Data.Seeds+=8+Math.Min(3,goals);p.TrainCooldown=1200;}}
            else if(p.PlayCooldown<=0) {p.Play++;p.Affection++;Data.Seeds+=8;p.PlayCooldown=1200;}
            p.Fatigue=Clamp(p.Fatigue+3);
        }
        public bool Buy(bool medicine) {int price=medicine?MedicinePrice:DewPrice;if(Data.Seeds<price)return false;Data.Seeds-=price;if(medicine)Data.Medicine++;else Data.Dew++;return true;}
        public void Kill(Pet p,string cause) {p.Dead=true;p.Active=false;p.Sleeping=false;p.GrowthReady=false;p.Cause=cause;p.DiedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm");}
        public static string TimeText(double seconds) {return TimeSpan.FromSeconds(Math.Max(0,seconds)).ToString(@"hh\:mm\:ss");}
        public static string Hint(Pet p) {if(p.Training>=12)return "훈련할 시간이 되면 먼저 준비하네요. 느긋하게만 지내지는 않을 것 같아요.";if(p.Training>p.Play && p.Training>p.Affection)return "작은 목표에도 눈을 떼지 않네요.";if(p.Play>p.Training)return "굴러가는 공을 보면 잎이 들썩여요.";if(p.Affection>0)return "당신이 다가오기를 기다리는 것 같아요.";return "아직 세상의 모든 것이 궁금한 아기예요.";}
    }
    public static class Storage {
        public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GuMaGoChi");
        public static string PathName {get {return Path.Combine(Folder,"save.json");}}
        static JavaScriptSerializer Serializer() {return new JavaScriptSerializer {MaxJsonLength=8*1024*1024};}
        public static string Encode(SaveData data) {return Serializer().Serialize(data);}
        public static SaveData Decode(string json) {
            SaveData d=Serializer().Deserialize<SaveData>(json);
            if(d==null || d.Version!=1 || d.Pets==null || d.Discovered==null || d.Seeds<0 || d.Dew<0 || d.Medicine<0 || d.Pets.Count>500)throw new InvalidDataException("지원하지 않거나 손상된 저장 데이터입니다.");
            var ids=new HashSet<string>();
            foreach(Pet p in d.Pets) {
                if(p==null || String.IsNullOrWhiteSpace(p.Id) || !ids.Add(p.Id) || String.IsNullOrWhiteSpace(p.Name) || p.Name.Length>20 || p.SpeciesId< -1 || p.SpeciesId>=Catalog.All.Length || p.PendingSpecies< -1 || p.PendingSpecies>=Catalog.All.Length || p.Age<0 || p.Age>Engine.Life || Double.IsNaN(p.Age) || Double.IsInfinity(p.Age))throw new InvalidDataException("개체 정보가 올바르지 않습니다.");
                if(new[]{p.Hunger,p.Dirt,p.Fatigue,p.Illness}.Any(v=>Double.IsNaN(v)||Double.IsInfinity(v)||v<0||v>100) || new[]{p.FoodCooldown,p.MedicineCooldown,p.PetCooldown,p.PlayCooldown,p.TrainCooldown,p.WasteClock,p.SleepClock,p.TalkClock}.Any(v=>Double.IsNaN(v)||Double.IsInfinity(v)) || p.Waste<0 || p.Waste>6 || (p.GrowthReady && p.PendingSpecies<0))throw new InvalidDataException("상태 정보가 올바르지 않습니다.");
                if(p.Friends==null)p.Friends=new Dictionary<string,int>();if(p.GrowthHistory==null)p.GrowthHistory=new List<int>();
            }
            if(d.Discovered.Any(i=>i<0||i>=Catalog.All.Length))throw new InvalidDataException("도감 정보가 올바르지 않습니다.");return d;
        }
        public static SaveData Load(out string warning) {
            warning=null;if(!File.Exists(PathName))return new SaveData();
            try {return Decode(File.ReadAllText(PathName));} catch(Exception) {
                try {SaveData backup=Decode(File.ReadAllText(PathName+".bak"));warning="저장 파일을 읽지 못해 이전 백업으로 복구했습니다.";return backup;}
                catch(Exception) {throw new InvalidDataException("저장 파일과 백업을 읽을 수 없습니다. 원본을 보존했습니다. 저장 폴더: "+Folder);}
            }
        }
        public static void Save(SaveData d) {Directory.CreateDirectory(Folder);string tmp=PathName+".tmp";File.WriteAllText(tmp,Encode(d),System.Text.Encoding.UTF8);if(File.Exists(PathName))File.Replace(tmp,PathName,PathName+".bak");else File.Move(tmp,PathName);}
    }
}
