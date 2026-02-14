using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class PlayVisualEffect : HNSFStateAction
    {
        public PlayVisualEffectRequestParam visualEffectRequestParam;
        public bool atClosestBodyPosition;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);

            var request = visualEffectRequestParam.Resolve(frame);
            var vfx = request.GetRngVFX(frame.RNG);
            
            VisualEffectHelper.PlayVisualEffect(
                frame: frame, 
                request: request, 
                vfx: vfx, 
                entity: entity, 
                position: transform->Position,
                rotation: transform->EulerAngles,
                closestBodyPosition: FPVector3.Zero,
                atClosestBodyPosition: atClosestBodyPosition);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new PlayVisualEffect());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as PlayVisualEffect;
            t.visualEffectRequestParam = visualEffectRequestParam;
            t.atClosestBodyPosition = atClosestBodyPosition;
            return base.CopyTo(target);
        }
    }
}