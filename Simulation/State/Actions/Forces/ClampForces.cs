using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Clamp Forces")]
    public unsafe partial class ClampForces : HNSFStateAction
    {
        public enum ForceGroupType
        {
            Movement,
            Gravity,
            Both
        }

        public ForceGroupType forceToClamp;
        public bool useMaxAsMin;
        public bool makeMinNegative;
        public HNSFParamFP minMagnitude;
        public HNSFParamFP maxMagnitude;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            
            var clampMagnitude = maxMagnitude.Resolve(frame, entity, ref stateContext);
            var minClamp = clampMagnitude;
            if(!useMaxAsMin) minClamp = minMagnitude.Resolve(frame, entity, ref stateContext);;
            if (makeMinNegative) minClamp *= -1;

            switch (forceToClamp)
            {
                case ForceGroupType.Movement:
                    physics->SetKinematicHorizontalSpeed(frame, entity, FPVector3.ClampMagnitude(physics->GetKinematicHorizontalSpeed(frame, entity), clampMagnitude));
                    break;
                case ForceGroupType.Gravity:
                    physics->SetDynamicVelocityVerticalSpeed(frame, entity, FPMath.Clamp(physics->GetDynamicVelocityVerticalSpeedFP(frame, entity), minClamp, clampMagnitude));
                    break;
                case ForceGroupType.Both:
                    physics->force = FPVector3.ClampMagnitude(physics->GetKinematicVelocity(frame, entity), clampMagnitude);
                    break;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ClampForces());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ClampForces;
            t.forceToClamp = forceToClamp;
            t.useMaxAsMin = useMaxAsMin;
            t.makeMinNegative = makeMinNegative;
            t.minMagnitude = minMagnitude.Clone() as HNSFParamFP;
            t.maxMagnitude = maxMagnitude.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}