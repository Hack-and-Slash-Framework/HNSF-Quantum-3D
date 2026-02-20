using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class SetLastHitByHitInfo : HNSFStateAction
    {
        public AssetRef<HitInfoBase> hitInfo;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var targetEntityRef = GetActionTargetEntityRef(frame, entity);
            if (targetEntityRef == EntityRef.None) return false;

            if (frame.Unsafe.TryGetPointer<LastHitByInfo>(targetEntityRef, out var lhbi))
            {
                lhbi->hitByInfo = hitInfo;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetLastHitByHitInfo());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetLastHitByHitInfo;
            t.hitInfo = hitInfo;
            return base.CopyTo(target);
        }
    }
}