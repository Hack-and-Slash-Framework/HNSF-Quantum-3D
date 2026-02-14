using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Apply Traction")]
    public unsafe partial class ApplyTraction : HNSFStateAction
    {
        public enum TractionType
        {
            Movement,
            FallSpeed,
            Both
        }

        public enum ModifierType
        {
            Add,
            Multiply
        }
        
        public enum CurveTimeType
        {
            StateTime,
            ActionRange
        }

        public TractionType tractionType;
        public ModifierType modifierType;
        public HNSFParamFP traction;
        public HNSFParamFP multiplier = FP._1;
        public bool useCurve;
        [DrawIf(nameof(useCurve), true)]
        public AnimationCurveParam tractionMultiplierCurve;
        [DrawIf(nameof(useCurve), true)]
        public CurveTimeType tractionCurveTimeType = CurveTimeType.StateTime;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var physics)) return false;

            FP t = traction.Resolve(frame, entity, ref stateContext) * multiplier.Resolve(frame, entity, ref stateContext);

            if (useCurve && tractionMultiplierCurve.TryResolve(frame, out var ac))
            {
                switch (tractionCurveTimeType)
                {
                    case CurveTimeType.StateTime:
                        var sTotalFrames = frame.FindAsset(stateContext.workingState).totalFrames;
                        t *= ac.Evaluate((FP)stateContext.stateFrame / (FP)sTotalFrames);
                        break;
                    case CurveTimeType.ActionRange:
                        t *= ac.Evaluate(rangePercent);
                        break;
                }
                if (t == FP._0) return false;
            }
            
            switch (tractionType)
            {
                case TractionType.Movement:
                    switch (modifierType)
                    {
                        case ModifierType.Add:
                            physics->SetKinematicHorizontalSpeed(frame, entity, FPVector3.MoveTowards(physics->GetKinematicHorizontalSpeed(frame, entity), FPVector3.Zero, t * frame.DeltaTime));
                            break;
                        case ModifierType.Multiply:
                            physics->SetKinematicHorizontalSpeed(frame, entity, physics->GetKinematicHorizontalSpeed(frame, entity) * t);
                            break;
                    }
                    break;
                case TractionType.FallSpeed:
                    physics->SetDynamicVelocityVerticalSpeed(frame, entity, FPMath.MoveTowards(physics->GetDynamicVelocityVerticalSpeedFP(frame, entity), 0, t * frame.DeltaTime));
                    break;
                case TractionType.Both:
                    var moveValue = physics->GetOverallVelocity(frame, entity);
                    moveValue = FPVector3.MoveTowards(moveValue, FPVector3.Zero, t * frame.DeltaTime);
                    physics->SetKinematicHorizontalSpeed(frame, entity, moveValue);
                    physics->SetDynamicVelocityVerticalSpeed(frame, entity, moveValue.Y);
                    break;
            }
            return false;
        }
        
        private FP MoveTowards(FP current, FP target, FP maxDelta)
        {
            if (FPMath.Abs(target - current) <= maxDelta) return target;
            return current + FPMath.Sign(target - current) * maxDelta;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ApplyTraction());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ApplyTraction;
            t.tractionType = tractionType;
            t.modifierType = modifierType;
            t.traction = traction.Clone() as HNSFParamFP;
            t.multiplier = multiplier.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}