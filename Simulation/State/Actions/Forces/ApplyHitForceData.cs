using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class ApplyHitForceData : HNSFStateAction
    {
        public enum ForceType
        {
            None,
            LastHitByForceGrounded,
            LastHitByForceAerial,
            LastHitByGroundBounce,
            LastHitByWallBounce,
            Custom = 100
        }

        public ForceType forceType;
        [DrawIf(nameof(forceType), (int)ForceType.Custom)] public HitForceData customForceData;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            HNSFStateContext targetStateContext = stateContext;
            var targetEntityRef = GetActionTargetEntityRef(frame, entity, ref targetStateContext);
            if (targetEntityRef == EntityRef.None) return false;
            DoAction(frame, targetEntityRef, ref targetStateContext);
            return false;
        }

        private void DoAction(Frame frame, EntityRef targetEntityRef, ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<LastHitWithInfo>(targetEntityRef, out var lastHitWithInfo)) return;
            if (lastHitWithInfo->data.Field != Quantum.LastHitWithData.HITINFODATA
                || !frame.TryFindAsset<HitInfo>(lastHitWithInfo->data.hitInfoData->hitWithInfo.Id, out var hitWithInfo)) return;

            HitForceData fgb = default;
            switch (forceType)
            {
                case ForceType.LastHitByForceGrounded:
                    fgb = hitWithInfo.forceGroundHit;
                    break;
                case ForceType.LastHitByForceAerial:
                    fgb = hitWithInfo.forceAerialHit;
                    break;
                case ForceType.LastHitByGroundBounce:
                    fgb = hitWithInfo.groundBounceForces;
                    break;
                case ForceType.LastHitByWallBounce:
                    fgb = hitWithInfo.wallBounceForces;
                    break;
                case ForceType.Custom:
                    fgb = customForceData;
                    break;
            }
            
            var hitstunTraction = fgb.hasCustomTraction ? (FP)fgb.traction : stateContext.aiConfig.Get("HitstunTraction").Value.FP;
            var hitstunAirFriction = fgb.hasCustomAirFriction ? (FP)fgb.friction : stateContext.aiConfig.Get("HitstunAirFriction").Value.FP;
            var hitstunGravity = fgb.hasCustomGravity ? fgb.hitstunGravity : stateContext.aiConfig.Get("HitstunGravity").Value.FP;
                    
            stateContext.blackboard->Set(frame, "Traction", hitstunTraction);
            stateContext.blackboard->Set(frame, "AirFriction", hitstunAirFriction);
            stateContext.blackboard->Set(frame, "Gravity", hitstunGravity);
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ApplyHitForceData());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ApplyHitForceData;
            t.forceType = forceType;
            t.customForceData = customForceData;
            return base.CopyTo(target);
        }
    }
}