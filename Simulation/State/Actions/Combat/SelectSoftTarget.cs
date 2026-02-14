using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class SelectSoftTarget : HNSFStateAction
    {
        public FP maxDistance = 5;
        public LayerMaskParam targetingLayerMask;
        public LayerMaskParam canSeeLayermask;
        public bool clearSoftTarget;
        public FP distanceFudge = FP.FromRaw(32768);
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var targeter)
                || targeter->hardLocked
                || !frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)
                || !frame.Unsafe.TryGetPointer<PhysicsCollider3D>(entity, out var physCollider)) return false;

            var lookingForward = transform->Forward;

            if (frame.Unsafe.TryGetPointer<ActorInputCamera>(entity, out var aii))
            {
                lookingForward = aii->GetForward(0);
                lookingForward.Y = 0;
                lookingForward = lookingForward.Normalized;
            }

            var selfCombatTeam = frame.Unsafe.GetPointer<CombatTeam>(entity);
        
            if(clearSoftTarget) targeter->softTarget = EntityRef.None;

            if (frame.Exists(targeter->softTarget)) return false;
        
            var phyCast = frame.Physics3D.OverlapShape(
                transform->Position,
                transform->Rotation,
                Shape3D.CreateSphere(maxDistance, physCollider->Shape.Centroid),
                targetingLayerMask.Get(frame),
                QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo);
        
            var bestTargetIndex = -1;
            var bestDistance = FP.UseableMax;
            var bestAngle = FP.UseableMax;
            
            for (int i = 0; i < phyCast.Count; i++)
            {
                if (phyCast[i].Entity == entity) continue;

                if (frame.Unsafe.TryGetPointer<CombatTeam>(phyCast[i].Entity, out var entityCombatTeam)
                    && !selfCombatTeam->IsHostileTowards(frame, entityCombatTeam->value)) continue;
            
                var directionVector = phyCast[i].Point - transform->Position;
                
                var raycastResult = frame.Physics3D.Raycast(
                    transform->Position + FPVector3.Up,
                    directionVector.Normalized,
                    directionVector.Magnitude,
                    canSeeLayermask.Get(frame),
                    QueryOptions.HitAll);
                if (raycastResult.HasValue) continue;

                var targetAngle = FPVector3.Angle(lookingForward, directionVector.Normalized);
                var dist = FPVector3.Distance(transform->Position, phyCast[i].Point);
                var distDifference = FPMath.Abs(dist - bestDistance);
            
                if (distDifference <= distanceFudge)
                {
                    if (targetAngle >= bestAngle) continue;
                    bestTargetIndex = i;
                    bestDistance = dist;
                    bestAngle = targetAngle;
                }else if (dist < bestDistance)
                {
                    bestTargetIndex = i;
                    bestDistance = dist;
                    bestAngle = targetAngle;
                }
            }

            if (bestTargetIndex == -1)
            {
                return false;
            }

            targeter->softTarget = phyCast[bestTargetIndex].Entity;
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SelectSoftTarget());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SelectSoftTarget;
            t.maxDistance = this.maxDistance;
            t.targetingLayerMask = this.targetingLayerMask.Clone();
            t.canSeeLayermask = this.canSeeLayermask.Clone();
            t.clearSoftTarget = this.clearSoftTarget;
            return base.CopyTo(target);
        }
    }
}