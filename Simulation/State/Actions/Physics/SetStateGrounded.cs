using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class SetStateGrounded : HNSFStateAction
    {
        public StateGroundedType groundedType;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var bap)) return false;
            bap->currentGroundedState = groundedType;
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetStateGrounded());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetStateGrounded;
            t.groundedType = groundedType;
            return base.CopyTo(target);
        }
    }
}