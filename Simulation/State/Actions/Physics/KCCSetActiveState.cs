using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/Set KCC Active State")]
    public unsafe partial class KCCSetActiveState : HNSFStateAction
    {
        public bool active = true;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var kcc = frame.Unsafe.GetPointer<KCC>(entity);
            kcc->SetActive(active);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new KCCSetActiveState());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as KCCSetActiveState;
            t.active = active;
            return base.CopyTo(target);
        }
    }
}