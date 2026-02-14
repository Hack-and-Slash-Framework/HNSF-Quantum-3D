using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Modify Forces")]
    public unsafe partial class MoveTowardsForce : HNSFStateAction
    {
        public enum InputSourceType
        {
            stick,
            rotation,
            slope,
            hardTarget,
            softTarget,
            custom
        }
        
        public InputSourceType[] inputSources = Array.Empty<InputSourceType>();
        public HNSFParamFP speedParam;
        public HNSFParamFP moveTowardsSpeedParam;
        public bool normalizeInput;
        public HNSFParamFPVector3 customInputParam;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var physicsBody)
                || !frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)) return false;
            
            FPVector3 input = FPVector3.Zero;

            for (int i = 0; i < inputSources.Length; i++)
            {
                switch (inputSources[i])
                {
                    case InputSourceType.slope:
                        break;
                    case InputSourceType.stick:
                        var bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(entity);
                        var bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(entity);
                        input = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), true);
                        break;
                    case InputSourceType.rotation:
                        input = transform->Forward;
                        break;
                    case InputSourceType.hardTarget:
                        var targeter = frame.Unsafe.GetPointer<CombatTargeter>(entity);
                        input = targeter->hardLocked ? targeter->lookForward : FPVector3.Zero;
                        break;
                    case InputSourceType.softTarget:
                        var t = frame.Unsafe.GetPointer<CombatTargeter>(entity);
                        if (frame.Exists(t->softTarget) &&
                            frame.Unsafe.TryGetPointer<Transform3D>(t->softTarget, out var softTargetTransform))
                        {
                            input = TransformHelpers.GetCenterPosition(frame, t->softTarget, softTargetTransform) - transform->Position;
                        }
                        break;
                    case InputSourceType.custom:
                        input = customInputParam.Resolve(frame, entity, ref stateContext);
                        break;
                }
                
                if (input != FPVector3.Zero) break;
            }

            if (normalizeInput && input != FPVector3.Zero) input = input.Normalized;

            var speed = speedParam.Resolve(frame, entity, ref stateContext);
            var moveTowardsSpeed = moveTowardsSpeedParam.Resolve(frame, entity, ref stateContext);
            
            physicsBody->SetOverallVelocity(frame, entity, FPVector3.MoveTowards(physicsBody->GetOverallVelocity(frame, entity), input * speed, moveTowardsSpeed * frame.DeltaTime));
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new MoveTowardsForce());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as MoveTowardsForce;
            t.inputSources = inputSources.ToArray();
            t.speedParam = speedParam.Clone() as HNSFParamFP;
            t.moveTowardsSpeedParam = moveTowardsSpeedParam.Clone() as HNSFParamFP;
            t.normalizeInput = normalizeInput;
            t.customInputParam = customInputParam.Clone() as HNSFParamFPVector3;
            return base.CopyTo(target);
        }
    }
}