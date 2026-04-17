using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.functions
{
    [System.Serializable]
    public unsafe partial class GetMovementVectorFromInput : StateFunctionFPVector3
    {
        public enum SourceType
        {
            Self,
            SoftTarget,
            HardTarget,
        }

        public SourceType[] eulerSource = Array.Empty<SourceType>();
        public bool useCustom;
        public HNSFParamFPVector2 inputCustom;
        
        public override FPVector3 Execute(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            FPVector3 dir = FPVector3.Zero;
            
            foreach (var pt in eulerSource)
            {
                switch (pt)
                {
                    case SourceType.Self:
                        dir = GetInput(frame, entity, ref stateContext);
                        break;
                    case SourceType.SoftTarget:
                        if (!frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var selfCombatTargeterB)
                            || !frame.Exists(selfCombatTargeterB->softTarget)) continue;
                        var stContext = new HNSFStateContext(frame, selfCombatTargeterB->softTarget);
                        dir = GetInput(frame, selfCombatTargeterB->softTarget, ref stContext);
                        break;
                    case SourceType.HardTarget:
                        if (!frame.Unsafe.TryGetPointer<CombatTargeter>(entity, out var selfCombatTargeterHard)
                            || !frame.Exists(selfCombatTargeterHard->targetEntity)) continue;
                        var htContext = new HNSFStateContext(frame, selfCombatTargeterHard->targetEntity);
                        dir = GetInput(frame, selfCombatTargeterHard->targetEntity, ref htContext);
                        break;
                }
                if(dir != FPVector3.Zero) break;
            }
            
            return dir;
        }

        public FPVector3 GetInput(Frame frame, EntityRef entityRef, ref HNSFStateContext stateContext)
        {
            if(!frame.Unsafe.TryGetPointer<ActorInputBufferMovement>(entityRef, out var bufferMovement)
               || !frame.Unsafe.TryGetPointer<ActorInputCamera>(entityRef, out var inputCamera)) return FPVector3.Zero;

            FPVector2 modiInput;
            if (useCustom)
                modiInput = inputCustom.Resolve(frame, entityRef, ref stateContext);
            else
                modiInput = bufferMovement->GetMovement(0);
            return InputHelper.GetMovementVector(inputCamera->GetForward(0), inputCamera->GetRight(0), modiInput.X, modiInput.Y);
        }

        public override HNSFStateFunction Copy()
        {
            return CopyTo(new GetMovementVectorFromInput());
        }

        public override HNSFStateFunction CopyTo(HNSFStateFunction target)
        {
            var t = target as GetMovementVectorFromInput;
            t.inputCustom = inputCustom.Clone() as HNSFParamFPVector2;
            return base.CopyTo(target);
        }
    }
}