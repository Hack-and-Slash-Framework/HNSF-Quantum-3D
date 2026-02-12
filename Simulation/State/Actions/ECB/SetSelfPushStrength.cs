using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/Push Strength")]
    public unsafe partial class SetSelfPushStrength : HNSFStateAction
    {
        public HNSFParamFP hardness = (FP)1;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var physics)) return false;
            physics->selfPushStrength = hardness.Resolve(frame, entity, ref stateContext);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetSelfPushStrength());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetSelfPushStrength;
            t.hardness = hardness.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}