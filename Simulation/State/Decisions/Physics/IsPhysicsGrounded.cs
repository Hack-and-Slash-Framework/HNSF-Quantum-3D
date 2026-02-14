using System;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class IsPhysicsGrounded : HNSFStateDecision
    {
        public enum CheckType
        {
            IsGrounded,
            IsAerial
        }

        public CheckType checkType;
        public bool checkLastFrame;

        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                bool isGrounded = kcc->IsGrounded || kcc->IsSteppingUp || kcc->IsSnappingToGround ||
                                  (checkLastFrame && kcc->Data.WasGrounded);
                
                switch (checkType)
                {
                    case CheckType.IsGrounded:
                        return isGrounded;
                    case CheckType.IsAerial:
                        return !isGrounded;
                }
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new IsPhysicsGrounded());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as IsPhysicsGrounded;
            t.checkType = checkType;
            t.checkLastFrame = checkLastFrame;
            return base.CopyTo(target);
        }
    }
}