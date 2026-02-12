using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Force Angle Target Adjustment")]
    public unsafe partial class ForceAngleTargetAdjustment : HNSFStateAction
    {
        public FP maxAngleAdjustment = 15;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            /*
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            var inputs = frame.Unsafe.GetPointer<ActorInputBuffer>(entity);
            var targeter = frame.Unsafe.GetPointer<CombatTargeter>(entity);

            EntityRef targetEntity = EntityRef.None;

            if (targeter->hardLocked == false && inputs->moveInput.SqrMagnitude <= 0 &&
                frame.Exists(targeter->softTarget))
            {
                targetEntity = targeter->softTarget;
            }else if (targeter->hardLocked && frame.Exists(targeter->targetEntity))
            {
                targetEntity = targeter->targetEntity;
            }

            if (targetEntity == EntityRef.None
                || !frame.Unsafe.TryGetPointer(targetEntity, out Transform3D* targetTransform)) return false;
            
            var force = physics->GetOverallVelocity(frame, entity);

            var wantedDir = (targetTransform->Position - transform->Position).Normalized;

            if (FPVector3.Angle(force.Normalized, wantedDir) > maxAngleAdjustment) return false;

            var mag = force.Magnitude;
            
            physics->SetOverallVelocity(frame, entity, wantedDir * mag);
            */
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ForceAngleTargetAdjustment());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ForceAngleTargetAdjustment;
            return base.CopyTo(target);
        }
    }
}