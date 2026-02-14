using System;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class HasWall : HNSFStateDecision
    {
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            return frame.Has<GotWallInfo>(entity);
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new HasWall());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as HasWall;
            return base.CopyTo(target);
        }
    }
}