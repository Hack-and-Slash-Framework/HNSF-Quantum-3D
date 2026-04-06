using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class PlaySound : HNSFStateAction
    {
        public FPVector3 positionOffset;

        public PlaySoundRequestParam playSoundRequestParam;
    
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);

            var soundRequest = playSoundRequestParam.Resolve(frame);
            var sound = soundRequest.GetRngSound(frame.RNG);
            if (!sound.soundRef.IsValid) return false;

            var position = transform->Position 
                           + transform->TransformDirection(soundRequest.positionOffset)
                           + transform->TransformDirection(positionOffset);
            SoundEffectHelper.PlaySound(frame, soundRequest, sound, entity, position);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new PlaySound());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as PlaySound;
            t.positionOffset = positionOffset;
            t.playSoundRequestParam = playSoundRequestParam;
            return base.CopyTo(target);
        }
    }
}