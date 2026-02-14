using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class StopVisualEffect : HNSFStateAction
    {
        public bool stopAllInstances;
        public AssetRef<VisualEffectEntry> effectToStop;
        public int offset;
        public bool destroyAllParticles;
        public bool unparent;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            frame.Events.StopVisualEffect(entity, stopAllInstances, destroyAllParticles, unparent, effectToStop, offset);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new StopVisualEffect());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as StopVisualEffect;
            t.stopAllInstances = stopAllInstances;
            t.effectToStop = effectToStop;
            t.offset = offset;
            t.destroyAllParticles = destroyAllParticles;
            t.unparent = unparent;
            return base.CopyTo(target);
        }
    }
}