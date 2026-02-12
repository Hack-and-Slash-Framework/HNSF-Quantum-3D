using System;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class IsStateGrounded : HNSFStateDecision
    {
        public enum CheckType
        {
            IsGrounded,
            IsAerial
        }

        public CheckType checkType;

        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var actorPhysics)) return false;
        
            switch (checkType)
            {
                case CheckType.IsGrounded:
                    return actorPhysics->currentGroundedState == StateGroundedType.GROUNDED;
                case CheckType.IsAerial:
                    return actorPhysics->currentGroundedState == StateGroundedType.AERIAL;
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new IsStateGrounded());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as IsStateGrounded;
            t.checkType = checkType;
            return base.CopyTo(target);
        }
    }
}