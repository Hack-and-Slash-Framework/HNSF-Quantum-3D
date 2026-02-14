using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Set Flight Movement")]
    public unsafe partial class SetFlightMovement : HNSFStateAction
    {
        public enum InputSourceType
        {
            none,
            stick,
            lookDirection,
            slope,
            custom,
            StickBuffered
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
        public int stickBufferedBuffer = 3;
        public bool flattenInput = true;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            HNSFStateContext targetStateContext = stateContext;
            var targetEntityRef = GetActionTargetEntityRef(frame, entity, ref targetStateContext);
            if (targetEntityRef == EntityRef.None) return false;
            
            BattleActorPhysics* actorPhysics = frame.Unsafe.GetPointer<BattleActorPhysics>(targetEntityRef);
            var transform = frame.Unsafe.GetPointer<Transform3D>(targetEntityRef);
            FPVector3 input = FPVector3.Zero;

            var speed = speedParam.Resolve(frame, targetEntityRef, ref stateContext);
            
            for (int i = 0; i < inputSources.Length; i++)
            {
                switch (inputSources[i])
                {
                    case InputSourceType.slope:
                        break;
                    case InputSourceType.stick:
                        var bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(targetEntityRef);
                        var bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(targetEntityRef);
                        input = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), false);
                        break;
                    case InputSourceType.lookDirection:
                        input = transform->Forward;
                        break;
                    case InputSourceType.custom:
                        input = customInput.Resolve(frame, targetEntityRef, ref stateContext);
                        break;
                    case InputSourceType.StickBuffered:
                        //input = InputHelper(frame, inputs, stickBufferedBuffer, 0, checkX: true, checkY: true);
                        break;
                }
                
                if (input != FPVector3.Zero) break;
            }

            if(flattenInput) input.Y = 0;
            if (normalizeInput && input != FPVector3.Zero) input = input.Normalized;
            
            if (modifyType == ModifyType.SET)
            {
                if (input == FPVector3.Zero)
                {
                    actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef, FPVector3.Zero);
                }
                else
                {
                    actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef,
                        new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input)));
                }
            }
            else
            {
                if (input == FPVector3.Zero) return false;
                actorPhysics->SetKinematicHorizontalSpeed(frame, targetEntityRef,
                    actorPhysics->GetKinematicHorizontalSpeed(frame, targetEntityRef) + new FPVector3(speed.X, 0, speed.Y).TransformDirection(FPQuaternion.LookRotation(input)) );
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new SetFlightMovement());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as SetFlightMovement;
            t.inputSources = inputSources.ToArray();
            t.speedParam = speedParam.Clone() as HNSFParamFPVector2;
            t.normalizeInput = normalizeInput;
            t.customInput = customInput.Clone() as HNSFParamFPVector3;
            t.stickBufferedBuffer = stickBufferedBuffer;
            return base.CopyTo(target);
        }
    }
}