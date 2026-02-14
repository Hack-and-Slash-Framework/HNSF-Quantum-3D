using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Modify Forces")]
    public unsafe partial class ModifyForces : HNSFStateAction
    {
        public enum InputSourceType
        {
            none,
            stick,
            rotation,
            slope,
            targetLook,
            custom,
            reversed
        }
        
        public enum ModifyType
        {
            SET,
            ADD
        }

        public ModifyType modifyType;
        public InputSourceType[] inputSources;
        public HNSFParamFP speedParam;
        public bool normalizeInput;
        public HNSFParamFPVector3 customInput;
        public bool includeYInReverse;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            HNSFStateContext targetStateContext = stateContext;
            var targetEntityRef = GetActionTargetEntityRef(frame, entity, ref targetStateContext);
            if (targetEntityRef == EntityRef.None) return false;
            ModifyForce(frame, targetEntityRef, ref targetStateContext);
            return false;
        }

        private void ModifyForce(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            var actorPhysics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            
            var speed = speedParam.Resolve(frame, entity, ref stateContext);
            
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
                    case InputSourceType.custom:
                        input = customInput.Resolve(frame, entity, ref stateContext);
                        break;
                    case InputSourceType.reversed:
                        input = actorPhysics->GetOverallVelocity(frame, entity);
                        input.X *= -1;
                        if (includeYInReverse) input.Y *= -1;
                        input.Z *= -1;
                        break;
                }
                
                if (input != FPVector3.Zero) break;
            }

            if (normalizeInput && input != FPVector3.Zero) input = input.Normalized;

            input *= speed;
            
            switch (modifyType)
            {
                case ModifyType.SET:
                    actorPhysics->SetOverallVelocity(frame, entity, input);
                    break;
                case ModifyType.ADD:
                    actorPhysics->SetOverallVelocity(frame, entity, actorPhysics->GetOverallVelocity(frame, entity) + input);
                    break;
            }
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ModifyForces());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ModifyForces;
            t.modifyType = modifyType;
            t.inputSources = inputSources.ToArray();
            t.speedParam = speedParam.Clone() as HNSFParamFP;
            t.normalizeInput = normalizeInput;
            t.customInput = customInput.Clone() as HNSFParamFPVector3;
            return base.CopyTo(target);
        }
    }
}