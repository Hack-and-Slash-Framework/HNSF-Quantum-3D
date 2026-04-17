using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Apply Movement")]
    public unsafe partial class ApplyMovement : HNSFStateAction
    {
        public enum InputSourceType
        {
            none,
            stick,
            rotation,
            slope,
            custom
        }

        public enum StickCameraSourceType
        {
            Camera,
            Wall,
            HardTarget,
            Rotation,
            Raw
        }

        public enum SurfaceType
        {
            Raw,
            Ground,
            Wall
        }

        public SurfaceType surfaceType = SurfaceType.Raw;
        public StickCameraSourceType stickCameraSource = StickCameraSourceType.Camera;
        public InputSourceType[] inputSources;
        public HNSFParamFP baseAcceleration;
        public HNSFParamFP acceleration;
        public HNSFParamFP deceleration;
        public HNSFParamFP decelerationOverMax;
        public HNSFParamFP minSpeed;
        public HNSFParamFP maxSpeed;
        public AssetRef curveRef;
        public HNSFParamFPVector3 customInput;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            var bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(entity);
            var bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(entity);
            AnimationCurveAsset curve = frame.FindAsset<AnimationCurveAsset>(curveRef.Id);
            var gotWallInfo = frame.Unsafe.TryGetPointer<GotWallInfo>(entity, out var gwi);
            
            FPVector3 input = FPVector3.Zero;
            
            for (int i = 0; i < inputSources.Length; i++)
            {
                switch (inputSources[i])
                {
                    case InputSourceType.stick:
                        switch (stickCameraSource)
                        {
                            case StickCameraSourceType.Camera:
                                input = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), false);
                                break;
                            case StickCameraSourceType.Wall:
                                if (gotWallInfo)
                                {
                                    input = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), true);
                                }
                                break;
                            case StickCameraSourceType.HardTarget:
                                if (frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var combatTargeter)
                                    && combatTargeter->hardLocked)
                                {
                                    input = bufferMovement->GetMovement(0).XOY;
                                    input = (combatTargeter->lookForward.XOZ.Normalized * input.Z) +
                                            (combatTargeter->lookRight.XOZ.Normalized * input.X);
                                }
                                break;
                            case StickCameraSourceType.Rotation:
                                input = bufferMovement->GetMovement(0).XOY;
                                input = (transform->Forward.XOZ.Normalized * input.Z) +
                                        (transform->Right.XOZ.Normalized * input.X);
                                break;
                            case StickCameraSourceType.Raw:
                                input = bufferMovement->GetMovement(0).XOY;
                                break;
                        }
                        break;
                    case InputSourceType.rotation:
                        input = transform->Forward;
                        break;
                    case InputSourceType.custom:
                        input = customInput.Resolve(frame, entity, ref stateContext);
                        break;
                }

                if (input != FPVector3.Zero) break;
            }

            switch (surfaceType)
            {
                case SurfaceType.Raw:
                    HandleMovement(frame, entity, physics, frame.DeltaTime, input,
                        baseAcceleration.Resolve(frame, entity, ref stateContext),
                        acceleration.Resolve(frame, entity, ref stateContext),
                        deceleration.Resolve(frame, entity, ref stateContext), 
                        decelerationOverMax.Resolve(frame, entity, ref stateContext), 
                        minSpeed.Resolve(frame, entity, ref stateContext), 
                        maxSpeed.Resolve(frame, entity, ref stateContext),
                        curve.animationCurve);
                    break;
                case SurfaceType.Ground:
                case SurfaceType.Wall:
                    if (gotWallInfo)
                    {
                        HandleProjectedMovement(frame, entity, physics,
                            gwi->wallNormal,
                            FPVector3.Cross(transform->Right, gwi->wallNormal),
                            frame.DeltaTime, input,
                            baseAcceleration.Resolve(frame, entity, ref stateContext),
                            acceleration.Resolve(frame, entity, ref stateContext),
                            deceleration.Resolve(frame, entity, ref stateContext),
                            decelerationOverMax.Resolve(frame, entity, ref stateContext),
                            minSpeed.Resolve(frame, entity, ref stateContext),
                            maxSpeed.Resolve(frame, entity, ref stateContext),
                            curve.animationCurve);
                    }
                    break;
            }
            return false;
        }

        private void HandleMovement(Frame frame, EntityRef entity, BattleActorPhysics* physics, FP deltaTime, FPVector3 movement, 
            FP baseAcceleration, FP acceleration, FP deceleration, FP decelerationOverMax, FP minSpeed, FP maxSpeed, FPAnimationCurve accelFromDot)
        {
            movement.Y.RawValue = FP.RAW_ZERO;
            
            if (movement.Magnitude > 1)
            {
                movement = movement.Normalized;
            }

            var horizontalSpeed = physics->GetKinematicHorizontalSpeed(frame, entity);
            
            FP realAcceleration = baseAcceleration + (movement.Magnitude * acceleration);

            // Calculated our wanted movement force.
            FP accel = 0;

            if (horizontalSpeed.SqrMagnitude > (maxSpeed * maxSpeed))
            {
                accel = decelerationOverMax;
            }
            else
            {
                accel = movement == FPVector3.Zero ? deceleration : realAcceleration * accelFromDot.Evaluate(FPVector3.Dot(movement.Normalized, horizontalSpeed));
            }

            FPVector3 goalVelocity = movement.Normalized * FPMath.Lerp(minSpeed, maxSpeed, movement.Magnitude / (FP)1 );
            
            var horizontalForces = FPVector3.MoveTowards(horizontalSpeed, goalVelocity, accel * deltaTime);
            physics->SetKinematicHorizontalSpeed(frame, entity, horizontalForces);
        }
        
        private void HandleProjectedMovement(Frame frame, EntityRef entity, BattleActorPhysics* physics, FPVector3 floorNormal, FPVector3 forwardAlignedToPlane, FP deltaTime, FPVector3 movement, 
            FP baseAcceleration, FP acceleration, FP deceleration, FP decelerationOverMax, FP minSpeed, FP maxSpeed, FPAnimationCurve accelFromDot)
        {
            var groundForwardRotation = FPQuaternion.LookRotation(forwardAlignedToPlane, floorNormal);
            
            var movementAlignedToPlane = FPVector3.ProjectOnPlane(movement, floorNormal);
            var movementLocalized = movementAlignedToPlane.InverseTransformDirection(groundForwardRotation);
            var currentForceLocalized = physics->GetKinematicVelocity(frame, entity).InverseTransformDirection(groundForwardRotation);

            movementLocalized.Y = 0;
            
            if (movementLocalized.Magnitude > 1)
            {
                movementLocalized = movementLocalized.Normalized;
            }
            
            FP realAcceleration = baseAcceleration + (movementLocalized.Magnitude * acceleration);

            // Calculated our wanted movement force.
            FP accel = 0;

            if (currentForceLocalized.SqrMagnitude > (maxSpeed * maxSpeed))
            {
                accel = decelerationOverMax;
            }
            else
            {
                accel = movementLocalized == FPVector3.Zero ? deceleration : realAcceleration * accelFromDot.Evaluate(FPVector3.Dot(movementLocalized.Normalized, currentForceLocalized));
            }

            FPVector3 goalVelocity = movementLocalized.Normalized * FPMath.Lerp(minSpeed, maxSpeed, movementLocalized.Magnitude / (FP)1 );
            
            var finalForces = FPVector3.MoveTowards(currentForceLocalized, goalVelocity, accel * deltaTime);
            
            
            physics->SetKinematicVelocity(frame, entity, finalForces.TransformDirection(groundForwardRotation));
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ApplyMovement());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ApplyMovement;
            t.surfaceType = surfaceType;
            t.stickCameraSource = stickCameraSource;
            t.inputSources = inputSources.ToArray();
            t.baseAcceleration = baseAcceleration.Clone() as HNSFParamFP;
            t.acceleration = acceleration.Clone() as HNSFParamFP;
            t.deceleration = deceleration.Clone() as HNSFParamFP;
            t.decelerationOverMax = decelerationOverMax.Clone() as HNSFParamFP;
            t.minSpeed = minSpeed.Clone() as HNSFParamFP;
            t.maxSpeed = maxSpeed.Clone() as HNSFParamFP;
            t.curveRef = curveRef;
            t.customInput = customInput.Clone() as HNSFParamFPVector3;
            return base.CopyTo(target);
        }
    }
}