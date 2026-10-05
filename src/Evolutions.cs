using System;
using System.Collections.Generic;
using System.Linq;

namespace GuMaGoChi {
    public sealed class Evolution {
        public readonly int Id,Parent;
        public readonly string Name,Asset,Personality;
        public Evolution(int id,int parent,string name,string asset,string personality){Id=id;Parent=parent;Name=name;Asset=asset;Personality=personality;}
    }
    public static class Evolutions {
        public static readonly Evolution[] All={
            new Evolution(0,30,"짭구마","jjapguma","완벽한 위장을 꿈꾸는 능청스러운 감자"),
            new Evolution(1,30,"찐감자","jjingamja","정체를 밝힌 당당하고 능청스러운 감자"),
            new Evolution(2,23,"호랑고마","horangoma","당당하고 도도한 호랑이 고구마"),
            new Evolution(3,23,"골골마","golgolma","능청스럽게 웃는 게으른 고양이 고구마"),
            new Evolution(4,24,"맹견마","maenggyeonma","묵직하게 집을 지키는 불독 고구마"),
            new Evolution(5,24,"치와마","chiwama","분노로 바들바들 떠는 치와와 고구마"),
            new Evolution(6,12,"해적선장마","pirate-captain","자신만만하게 동료를 이끄는 고구마 선장"),
            new Evolution(7,12,"유령해적마","ghost-pirate","유령 불꽃으로 장난치는 능청스러운 해적 고구마")
        };
        public static bool Valid(int id,int parent){return id>=0&&id<All.Length&&All[id].Parent==parent;}
        public static IEnumerable<Evolution> For(int parent){return All.Where(e=>e.Parent==parent);}
        public static bool CanChange(Pet p){return p!=null&&p.Active&&!p.Dead&&!p.Sleeping&&!p.GrowthReady&&For(p.SpeciesId).Any();}
        // Design-preview release: no unapproved age/activity gates or combat bonuses.
        // Choices are reversible while the final evolution rules are being designed.
        public static bool Change(SaveData data,Pet p,int id) {
            if(data==null||!data.Pets.Contains(p)||!CanChange(p)||id==p.EvolutionId||(id!=-1&&!Valid(id,p.SpeciesId)))return false;
            p.EvolutionId=id;
            if(id>=0&&!data.DiscoveredEvolutions.Contains(id))data.DiscoveredEvolutions.Add(id);
            return true;
        }
        public static string Status(Pet p){return p.EvolutionId>=0?"2차 진화 · 모습 선택 가능":For(p.SpeciesId).Any()?"2차 진화: 행동 메뉴에서 모습 선택":"추가 성장: 추후 업데이트";}
        public static string RevealLine(Pet p) {
            switch(p.EvolutionId){
                case 0:return "어디서 봐도 고구마구마! 뒤쪽은 보지 말구마!";
                case 1:return "맞구마. 사실 감자였구마. 그래도 나는 나구마!";
                case 2:return "내 줄무늬 봤지? 이제 내가 앞에 설게.";
                case 3:return "히히… 이 모습이면 가만히 있어도 즐겁겠네.";
                case 4:return "여긴 내가 지킨다. 뒤로 물러나 있어.";
                case 5:return "작다고 얕보지 마! 지금 엄청 화났거든!";
                case 6:return "닻을 올려라! 오늘의 모험은 내가 이끈다!";
                case 7:return "으흐흐… 보물을 숨긴 곳은 나만 알지!";
                default:return "다시 익숙한 모습으로 돌아왔어.";
            }
        }
    }
}
