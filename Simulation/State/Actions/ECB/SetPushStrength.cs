using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/Push Strength")]
    public unsafe partial class SetPushStrength : HNSFStateAction
    {
        public HNSFParamFP hardness = (FP)1;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var physics)) return false;
            physics->pushStrength = hardness.Resolve(frame, entity, ref stateContext);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetPushStrength());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetPushStrength;
            t.hardness = hardness.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}