using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class CompareFallSpeed : HNSFStateDecision
    {
        public enum CheckType
        {
            IS_NEGATIVE,
            IS_POSITIVE,
            IS_ZERO_EPSILON,
            GREATER_THAN_VALUE,
            LESS_THAN_VALUE,
            IS_NEGATIVE_OR_ZERO
        }

        public CheckType checkType;
        public FP epsilon = FP.Epsilon;
        
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var cphys = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
        
            switch (checkType)
            {
                case CheckType.IS_NEGATIVE:
                    return cphys->GetOverallVelocity(frame, entity).Y < 0;
                case CheckType.IS_NEGATIVE_OR_ZERO:
                    return cphys->GetOverallVelocity(frame, entity).Y <= 0;
                case CheckType.IS_POSITIVE:
                    return cphys->GetOverallVelocity(frame, entity).Y > 0;
                case CheckType.IS_ZERO_EPSILON:
                    return FPMath.Abs(cphys->GetOverallVelocity(frame, entity).Y) <= epsilon;
                case CheckType.GREATER_THAN_VALUE:
                    return cphys->GetOverallVelocity(frame, entity).Y > epsilon;
                case CheckType.LESS_THAN_VALUE:
                    return cphys->GetOverallVelocity(frame, entity).Y < epsilon;
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new CompareFallSpeed());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as CompareFallSpeed;
            t.checkType = checkType;
            t.epsilon = epsilon;
            return base.CopyTo(target);
        }
    }
}