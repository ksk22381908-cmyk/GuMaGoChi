using System;

namespace GuMaGoChi {
    public enum RunnerObstacleKind {Crawler,Grasshopper,Mole}
    public enum RunnerObstacleState {Cruise,Warning,Active,Recovery,Hidden}
    public sealed class RunObstacle {
        public double Distance,Clock;
        public RunnerObstacleKind Kind;
        public RunnerObstacleState State;
        public bool Pending,Triggered,Encountered;
        public bool Visible {get{return !Pending&&(Kind!=RunnerObstacleKind.Mole||State==RunnerObstacleState.Warning||State==RunnerObstacleState.Active||State==RunnerObstacleState.Recovery);}}
        public bool Collidable {get{return !Pending&&(Kind!=RunnerObstacleKind.Mole||(State==RunnerObstacleState.Active&&Emergence>=.25));}}
        public double Height {get{return Kind==RunnerObstacleKind.Grasshopper&&State==RunnerObstacleState.Active?110*Math.Sin(Math.PI*Math.Min(1,Clock/1.05)):0;}}
        public double Emergence {get{return Kind!=RunnerObstacleKind.Mole?1:State==RunnerObstacleState.Active?Math.Min(1,Math.Min(Clock/.15,(.7-Clock)/.15)):0;}}
        public void Update(double dt,double ahead,double approachSpeed,double warningTime){
            if(Pending||Kind==RunnerObstacleKind.Crawler)return;
            if(State==RunnerObstacleState.Cruise&&!Triggered&&ahead<approachSpeed*(warningTime+(Kind==RunnerObstacleKind.Grasshopper?.5:.18))){State=RunnerObstacleState.Warning;Clock=0;Triggered=true;}
            if(State==RunnerObstacleState.Warning){Clock+=dt;if(Clock>=warningTime){Clock-=warningTime;State=RunnerObstacleState.Active;}}
            else if(State==RunnerObstacleState.Active){Clock+=dt;if(Clock>=(Kind==RunnerObstacleKind.Grasshopper?1.05:.7)){State=RunnerObstacleState.Recovery;Clock=0;}}
            else if(State==RunnerObstacleState.Recovery){Clock+=dt;if(Clock>.35)State=RunnerObstacleState.Cruise;}
        }
    }
}
