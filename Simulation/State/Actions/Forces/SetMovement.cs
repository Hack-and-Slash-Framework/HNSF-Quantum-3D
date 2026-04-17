using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Set Movement")]
    public unsafe partial class SetMovement : HNSFStateAction
    {
        public enum InputSourceType
        {
            none,
            stick,
            lookDirection,
            slope,
            custom,
            bufferedStickMovement,
            lookDirectionWithCustomInput
        }
        
        public enum ModifyType
        {
            SET,
            ADD
        }

        public ModifyType modifyType;
        public InputSourceType[] inputSources;
        public HNSFParamFPVector2 speedParam;
        public bool normalizeInput;
        public HNSFParamFPVector3 customInput;
        public short stickBufferedBuffer = 0;
        public bool asFlight = false;

        public bool multiplyByCurve;
        [DrawIf(nameof(multiplyByCurve), true)]
        public HNSFParamAssetRef curveAssetRef;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            HNSFStateContext targetStateContext = stateContext;
            var targetEntityRef = GetActionTargetEntityRef(frame, entity, ref targetStateContext);
            if (targetEntityRef == EntityRef.None) return false;
            
            BattleActorPhysics* actorPhysics = frame.Unsafe.GetPointer<BattleActorPhysics>(targetEntityRef);
            var transform = frame.Unsafe.GetPointer<Transform3D>(targetEntityRef);
            FPVector3 input = FPVector3.Zero;

            var speed = speedParam.Resolve(frame, targetEntityRef, ref targetStateContext);
            
            for (int i = 0; i < inputSources.Length; i++)
            {
                ActorInputCamera* bufferCam;
                ActorInputBufferMovement* bufferMovement;
                switch (inputSources[i])
                {
                    case InputSourceType.slope:
                        break;
                    case InputSourceType.stick:
                        bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(targetEntityRef);
                        bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(targetEntityRef);
                        input = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), !asFlight);
                        break;
                    case InputSourceType.lookDirection:
                        input = transform->Forward;
                        break;
                    case InputSourceType.custom:
                        input = customInput.Resolve(frame, targetEntityRef, ref targetStateContext);
                        break;
                    case InputSourceType.bufferedStickMovement:
                        bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(targetEntityRef);
                        bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(targetEntityRef);
                        input = bufferCam->GetMovementVector(0, bufferMovement->GetFirstMovementInput(stickBufferedBuffer, FP.SmallestNonZero), !asFlight);
                        break;
                    case InputSourceType.lookDirectionWithCustomInput:
                        var ci = customInput.Resolve(frame, targetEntityRef, ref targetStateContext);
                        input = (transform->Forward * ci.Z) + (transform->Right * ci.X) + (transform->Up * ci.Y);
                        break;
                }
                
                if (input != FPVector3.Zero) break;
            }

            if(asFlight == false) input.Y = 0;
            if (normalizeInput && input != FPVector3.Zero) input = input.Normalized;

            if (multiplyByCurve && frame.TryFindAsset(curveAssetRef.Resolve(frame, targetEntityRef, ref targetStateContext),
                    out AnimationCurveAsset curveAsset)
                && frame.TryFindAsset(targetStateContext.workingState, out var ws))
            {
                
                speed *= curveAsset.animationCurve.Evaluate((FP)targetStateContext.stateFrame / (FP)ws.totalFrames);
            }
            
            if (modifyType == ModifyType.SET)
            {
                if (input == FPVector3.Zero)
                {
                    if(asFlight) actorPhysics->SetOverallVelocity(frame, targetEntityRef, FPVector3.Zero);
                    else actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef, FPVector3.Zero);
                }
                else
                {
                    if(asFlight) actorPhysics->SetOverallVelocity(frame, targetEntityRef, new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input)));
                    else actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef,
                        new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input)));
                }
            }
            else
            {
                if (input == FPVector3.Zero) return false;
                if (asFlight)
                {
                    actorPhysics->SetOverallVelocity(frame, targetEntityRef,
                        actorPhysics->GetOverallVelocity(frame, targetEntityRef)
                        +  new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input))
                        );
                }
                else
                {
                    actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef,
                        actorPhysics->GetKinematicHorizontalSpeed(frame, targetEntityRef) +
                        new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input)));
                }
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetMovement());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetMovement;
            t.inputSources = inputSources.ToArray();
            t.speedParam = speedParam.Clone() as HNSFParamFPVector2;
            t.normalizeInput = normalizeInput;
            t.customInput = customInput.Clone() as HNSFParamFPVector3;
            t.stickBufferedBuffer = stickBufferedBuffer;
            return base.CopyTo(target);
        }
    }
}