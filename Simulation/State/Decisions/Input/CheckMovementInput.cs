using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class CheckMovementInput : HNSFStateDecision
    {
        public enum CheckType
        {
            IsMoving,
            IsNotMoving,
            Values
        }

        public CheckType checkType;
        public FP minValue;
        public FP maxValue;
        public bool alsoCheckLastInput;
        public int lastInputBufferOffset = 0;
        
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var inputs = frame.Unsafe.GetPointer<ActorInputBufferMovement>(entity);
            if (inputs->disableReadMovement) return false;

            var moveInput = inputs->GetMovement(0);
            var lastMoveInput = inputs->GetMovement(1);
            var moveMag = moveInput.SqrMagnitude;
            var minValueSqr = minValue * minValue;
            
            switch (checkType)
            {
                case CheckType.IsMoving:
                    var lastIs = !alsoCheckLastInput || lastMoveInput.SqrMagnitude >= minValueSqr;
                    return moveMag >= minValueSqr && lastIs;
                case CheckType.IsNotMoving:
                    var lastNot = !alsoCheckLastInput || lastMoveInput.SqrMagnitude <= minValueSqr;
                    return moveMag <= minValueSqr && lastNot;
                case CheckType.Values:
                    var maxValueSqr = maxValue * maxValue;
                    if (moveMag < (minValueSqr) ||
                        moveMag >= (maxValueSqr)) return false;
                    return true;
            }
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new CheckMovementInput());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as CheckMovementInput;
            t.checkType = checkType;
            t.minValue = minValue;
            t.maxValue = maxValue;
            t.alsoCheckLastInput = alsoCheckLastInput;
            t.lastInputBufferOffset = lastInputBufferOffset;
            return base.CopyTo(target);
        }
    }
}