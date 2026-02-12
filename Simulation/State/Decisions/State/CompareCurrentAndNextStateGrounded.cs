using System;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class CompareCurrentAndNextStateGrounded : HNSFStateDecision
    {
        public enum CheckType
        {
            EQUAL,
            NOT_EQUAL,
        }

        public CheckType checkType;
    
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var bap)
                || !stateContext.agentData->toStateRequested
                || !frame.TryFindAsset(stateContext.agentData->toState, out var toState)) return false;

            switch (checkType)
            {
                case CheckType.EQUAL:
                    return bap->currentGroundedState == toState.initialGroundedState;
                case CheckType.NOT_EQUAL:
                    return bap->currentGroundedState != toState.initialGroundedState;
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new CompareCurrentAndNextStateGrounded());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as CompareCurrentAndNextStateGrounded;
            t.checkType = checkType;
            return base.CopyTo(target);
        }
    }
}