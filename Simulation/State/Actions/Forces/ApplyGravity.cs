using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Apply Gravity")]
    public unsafe partial class ApplyGravity : HNSFStateAction
    {

        public HNSFParamFP gravity;
        public HNSFParamFP maxFallSpeed;
        public bool applyCurve;
        public AssetRef<AnimationCurveAsset> gravityCurve;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            
            physics->SetDynamicVelocityVerticalSpeed(frame, entity, 
                FPMath.MoveTowards(
                    physics->GetDynamicVelocityVerticalSpeedFP(frame, entity), 
                    -maxFallSpeed.Resolve(frame,entity, ref stateContext), 
                    gravity.Resolve(frame, entity, ref stateContext) * frame.DeltaTime)
            );
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ApplyGravity());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ApplyGravity;
            t.gravity = gravity.Clone() as HNSFParamFP;
            t.maxFallSpeed = maxFallSpeed.Clone() as HNSFParamFP;
            t.applyCurve = applyCurve;
            t.gravityCurve = gravityCurve;
            return base.CopyTo(target);
        }
    }
}