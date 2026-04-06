using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Rotation/Rotate Towards Input")]
    public unsafe partial class RotateTowardsInput : HNSFStateAction
    {
        public int throweeId;

        public enum RotateTowardsType
        {
            stick,
            lock_on_target,
            movement,
            soft_target,
            look_direction,
            wall
        }

        public enum RotateSetType
        {
            rotateTowards,
            Set
        }

        public RotateSetType rotationType = RotateSetType.rotateTowards;
        public RotateTowardsType[] rotateTowards;
        public HNSFParamFP rotationSpeedParam;
        public bool reverse = false;
        public bool useY;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            var hasInputs = frame.Unsafe.TryGetPointer<ActorInputBufferMovement>(entity, out var inputs);
            var hasCam = frame.Unsafe.TryGetPointer<ActorInputCamera>(entity, out var inputCam);
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);

            var wantedDir = FPVector3.Zero;

            for (int i = 0; i < rotateTowards.Length; i++)
            {
                switch (rotateTowards[i])
                {
                    case RotateTowardsType.stick:
                        if (hasInputs)
                        {
                            var lookForward = inputCam->GetForward();
                            var lookRight = inputCam->GetRight();
                            wantedDir = inputs->GetMovement().XOY;
                            wantedDir = InputHelper.GetMovementVector(lookForward, lookRight, wantedDir.X, wantedDir.Z);
                        }
                        break;
                    case RotateTowardsType.movement:
                        wantedDir = physics->GetKinematicHorizontalSpeed(frame, entity);
                        break;
                    case RotateTowardsType.soft_target:
                        if (!frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var targeter)
                            || !frame.Exists(targeter->softTarget))
                            break;
                        wantedDir = TransformHelpers.GetCenterOfMass(frame, targeter->softTarget) - transform->Position;
                        if(useY == false) wantedDir.Y = 0;
                        break;
                    case RotateTowardsType.lock_on_target:
                        if (!frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var targeterAlt)
                            || targeterAlt->hardLocked == false)
                            break;
                        wantedDir = targeterAlt->lookForward;
                        if(useY == false) wantedDir.Y = 0;
                        break;
                    case RotateTowardsType.look_direction:
                        wantedDir = inputCam->GetForward();
                        if(useY == false) wantedDir.Y = 0;
                        break;
                    case RotateTowardsType.wall:
                        if (!frame.Unsafe.TryGetPointer<GotWallInfo>(entity, out var wallInfo)) break;
                        wantedDir = -wallInfo->wallNormal;
                        break;
                }
                
                if(wantedDir != FPVector3.Zero) break;
            }

            if (wantedDir == FPVector3.Zero) return false;
            wantedDir = wantedDir.Normalized;
            
            if(reverse) wantedDir = -wantedDir;

            var hasKcc = frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc);
            
            switch (rotationType)
            {
                case RotateSetType.rotateTowards:
                    var rotationSpeed = rotationSpeedParam.Resolve(frame, entity, ref stateContext);
                    var resultRotation = FPQuaternion.RotateTowards(transform->Rotation,
                        FPQuaternion.LookRotation(wantedDir), rotationSpeed * frame.DeltaTime);
                    if (hasKcc)
                    {
                        kcc->SetLookRotation(resultRotation);
                    }

                    transform->Rotation = resultRotation;
                    break;
                case RotateSetType.Set:
                    var resultRotation2 = FPQuaternion.LookRotation(wantedDir);
                    if (hasKcc)
                    {
                        kcc->SetLookRotation(resultRotation2);
                    }
                    transform->Rotation = resultRotation2;   
                    break;
            }
            
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new RotateTowardsInput());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as RotateTowardsInput;
            t.throweeId = throweeId;
            t.rotateTowards = new RotateTowardsType[rotateTowards.Length];
            Array.Copy(rotateTowards, t.rotateTowards, rotateTowards.Length);
            t.rotationSpeedParam = rotationSpeedParam;
            t.reverse = reverse;
            t.useY = useY;
            return base.CopyTo(target);
        }
    }
}