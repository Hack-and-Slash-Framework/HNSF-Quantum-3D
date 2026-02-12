using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/ECB/Snap to ECB")]
    public unsafe partial class SnapToECB : HNSFStateAction
    {
        public bool asTeleport = true;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc)
                && frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
            {

                if (asTeleport)
                    transform->Teleport(frame,
                        transform->Position + transform->TransformDirection(kcc->Data.PositionOffset));
                else transform->Position += transform->TransformDirection(kcc->Data.PositionOffset);
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SnapToECB());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SnapToECB;
            t.asTeleport = asTeleport;
            return base.CopyTo(target);
        }
    }
}