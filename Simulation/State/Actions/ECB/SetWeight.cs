using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Physics/Weight")]
    public unsafe partial class SetWeight : HNSFStateAction
    {
        public HNSFParamFP hardness = (FP)1;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var physics)) return false;
            physics->weight = hardness.Resolve(frame, entity, ref stateContext);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetWeight());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetWeight;
            t.hardness = hardness.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}