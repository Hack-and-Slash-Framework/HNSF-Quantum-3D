using System;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class CheckDeathStatus : HNSFStateDecision
    {
        public enum WantedStatus
        {
            IsDead,
            IsAlive
        }
        
        public WantedStatus checkStatus = WantedStatus.IsDead;
        
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var rr = frame.Has<IsDead>(entity);
            switch (checkStatus)
            {
                case WantedStatus.IsDead:
                    return rr;
                case WantedStatus.IsAlive:
                    return !rr;
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new CheckDeathStatus());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as CheckDeathStatus;
            t.checkStatus = checkStatus;
            return base.CopyTo(target);
        }
    }
}