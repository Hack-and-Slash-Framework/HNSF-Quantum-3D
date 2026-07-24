using System;
using System.Linq;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class CauseScreenShake : HNSFStateAction
    {
        public ScreenShakeRequestParam[] screenShakes = Array.Empty<ScreenShakeRequestParam>();
        public bool isGlobal;
        public HNSFParamEntityRef[] targetEntities = Array.Empty<HNSFParamEntityRef>();
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            
            foreach (var shakeEvent in screenShakes)
            {
                var shakeRequest = shakeEvent.Resolve(frame);

                if (isGlobal)
                {
                    DoShakeEvent(frame, shakeRequest, transform, shakeEvent, default, true);
                }
                else
                {
                    if (targetEntities.Length == 0)
                    {
                        if(TryGetPlayerRef(frame, entity, out PlayerRef playerRef)) 
                            DoShakeEvent(frame, shakeRequest, transform, shakeEvent, playerRef, isGlobal: false);
                    }
                    else
                    {
                        foreach (var targetEntityRefParam in targetEntities)
                        {
                            var targetEntityRef = targetEntityRefParam.Resolve(frame, entity, ref stateContext);
                            
                            if(TryGetPlayerRef(frame, targetEntityRef, out PlayerRef playerRef)) 
                                DoShakeEvent(frame, shakeRequest, transform, shakeEvent, playerRef, isGlobal: false);
                        }
                    }
                }
            }
            return false;
        }

        private bool TryGetPlayerRef(Frame frame, EntityRef entityRef, out PlayerRef playerRef)
        {
            playerRef = default;
            if (!frame.Exists(entityRef)) return false;
            if (!frame.Unsafe.TryGetPointer<PlayerLink>(entityRef, out var playerLink)) return false;
            playerRef = playerLink->Player;
            return true;
        }

        private EventCauseScreenShake DoShakeEvent(Frame frame, ScreenShakeRequest shakeRequest, Transform3D* transform, ScreenShakeRequestParam shakeEvent, PlayerRef playerRef, bool isGlobal = false)
        {
            return frame.Events.CauseScreenShake(
                shakeType: shakeRequest.shakeType,
                isGlobal: isGlobal,
                origin: transform->Position.XYO,
                shakeAmount: shakeRequest.cameraShakeAmount,
                shakeSpeed: shakeRequest.cameraShakeSpeed,
                shakeFrames: shakeEvent is { type: ScreenShakeRequestParam.ParamType.External, screenShakeFramesOverride: > 0 } ? shakeEvent.screenShakeFramesOverride : shakeRequest.cameraShakeFrames,
                shakeInterval: shakeRequest.shakeInterval,
                onlyFor: playerRef);
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new CauseScreenShake());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as CauseScreenShake;
            t.screenShakes = screenShakes.ToArray();
            t.isGlobal = isGlobal;
            t.targetEntities = new HNSFParamEntityRef[targetEntities.Length];
            for (int i = 0; i < targetEntities.Length; i++)
            {
                t.targetEntities[i] = targetEntities[i].Clone() as HNSFParamEntityRef;
            }
            return base.CopyTo(target);
        }
    }
}