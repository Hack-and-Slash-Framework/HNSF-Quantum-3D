using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Force Unground")]
    public unsafe partial class ForceUnground : HNSFStateAction
    {
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->Data.IsGrounded = false;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ForceUnground());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ForceUnground;
            return base.CopyTo(target);
        }
    }
}