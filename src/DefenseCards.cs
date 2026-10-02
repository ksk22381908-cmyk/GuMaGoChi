using System;
using System.Collections.Generic;
using System.Linq;

namespace GuMaGoChi {
    public class DefenseCard {
        public readonly int Kind;public readonly Defender Target;
        public DefenseCard(int kind,Defender target){Kind=kind;Target=target;}
        public string Scope {get{return Target==null?"전체 고구마":(Target.Slot+1)+"번 · "+Target.Pet.Name;}}
        public string Title {get{return new[]{"힘 키우기","함께 힘내기","빠른 손놀림","박자 맞추기","필살기 연습","합동 필살기","개인 집중","팀워크"}[Kind];}}
        public string Description {get{return new[]{"기본 공격·스킬 피해 +30%","기본 공격·스킬 피해 +12%","공격속도 +25%","공격속도 +10%","스킬 피해 +35%","스킬 피해 +15%","스킬 쿨타임 -20%","스킬 쿨타임 -10%"}[Kind]+(Kind/2==1?"\n최대 공격속도: 초기의 4배":Kind/2==3?"\n최소 쿨타임: 5초":"");}}
    }
    // Slot identity survives empty gaps and swapping; one pet cannot occupy two slots.
    public class DefenseRoster {
        public readonly Pet[] Slots=new Pet[5];
        public DefenseRoster(IEnumerable<Pet> pets){int i=0;foreach(var p in pets.Where(p=>!p.Dead).GroupBy(p=>p.Id).Select(g=>g.First()).Take(5))Slots[i++]=p;}
        public bool Assign(int slot,Pet pet){if(slot<0||slot>=5||pet==null||pet.Dead)return false;int previous=Array.FindIndex(Slots,p=>p!=null&&p.Id==pet.Id);if(previous==slot)return true;Pet displaced=Slots[slot];if(previous>=0)Slots[previous]=displaced;Slots[slot]=pet;return true;}
        public bool Clear(int slot){if(slot<0||slot>=5)return false;Slots[slot]=null;return true;}
        public IEnumerable<Pet> Pets {get{return Slots.Where(p=>p!=null&&!p.Dead);}}
        public void ApplySlots(DefenseBattle battle){foreach(var d in battle.Team)d.Slot=Array.FindIndex(Slots,p=>p!=null&&p.Id==d.Pet.Id);}
    }
}
