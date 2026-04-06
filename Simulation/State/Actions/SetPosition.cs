using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Position/Set Position")]
    public unsafe partial class SetPosition : HNSFStateAction
    {
        public FPVector3 offset;
        public int throweeId;

        public StateActionTargetContext targetContext;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<Transform3D>(entity, out var originPointTransform)) return false;

            targetContext.callingEntity = entity;
            var targetEntityRef = HNSFStateHelper.GetStateTargetEntity(frame, ref targetContext);
            if (targetEntityRef == EntityRef.None) return false;
            if (!frame.Unsafe.TryGetPointer<Transform3D>(targetEntityRef, out var targetTransform)) return false;
            targetTransform->Position = originPointTransform->Position +
                                        originPointTransform->TransformDirection(offset);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetPosition());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetPosition;
            t.offset = offset;
            t.throweeId = throweeId;
            return base.CopyTo(target);
        }
    }
}