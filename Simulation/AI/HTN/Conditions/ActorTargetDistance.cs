using System;
using Quantum;
using HnSF.core.AI.HTN.Conditions;
using HnSF.core.AI.HTN.Param;
using Photon.Deterministic;
#if QUANTUM_UNITY
using UnityEngine;
#endif
#if UNITY_EDITOR
using HnSF.Nodes;
#endif

namespace HnSF.core.AI.HTN.Conditions
{
    [Serializable]
    public unsafe partial class ActorTargetDistance : ICondition
    {
        [field: SerializeField] public string Label { get; set; } = "";

        public ComparisonType comparison;
        public HTNParamFP minParam;
        public HTNParamFP maxParam;
        
        public bool IsValid(ref HTNAgentContext context)
        {
            var frame = context.frame;
            if (!frame.Unsafe.TryGetPointer<BattleActorAI>(context.agentEntityRef, out var battleActorAI)
                || !frame.Unsafe.TryGetPointer<EntityTargeting>(battleActorAI->target, out var targeting)) return false;
            
            var selfTransform = frame.Unsafe.GetPointer<Transform3D>(battleActorAI->target);
            var targetTransform = frame.Unsafe.GetPointer<Transform3D>(targeting->target);
            
            var dist = FPVector3.DistanceSquared(selfTransform->Position, targetTransform->Position);
            
            var min = minParam.Resolve(ref context);
            min *= min;

            switch (comparison)
            {
                case ComparisonType.Inbetween:
                    var max = maxParam.Resolve(ref context);
                    max *= max;
                    return dist >= min && dist <= max;
                case ComparisonType.Equals:
                    return dist == min;
                case ComparisonType.MoreThan:
                    return dist > min;
                case ComparisonType.MoreThanOrEqualTo:
                    return dist >= min;
                case ComparisonType.LessThan:
                    return dist < min;
                case ComparisonType.LessThanOrEqualTo:
                    return dist <= min;
            }
            return false;
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.AI.HTN.Nodes
{
    [Serializable]
    internal class ConditionActorTargetDistanceNode : ConditionBase
    {
        public const string optionComparisonType = "Comparison";
        public const string inParamMin = "MinParam";
        public const string inParamMax = "MaxParam";
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            
            context.AddOption<ComparisonType>(optionComparisonType)
                .WithDisplayName("Comparison")
                .WithDefaultValue(ComparisonType.Equals)
                .Build();
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            base.OnDefinePorts(context);

            context.AddInputPort(inParamMin)
                .WithDisplayName("Min")
                .Build();
            
            context.AddInputPort(inParamMax)
                .WithDisplayName("Max")
                .Build();
        }

        public override ICondition Convert()
        {
            //this.GetNodeOptionByName(OPTION_LABEL).TryGetValue<string>(out var label);
            GetNodeOptionByName(optionComparisonType).TryGetValue<ComparisonType>(out var comparisonType);
            
            return new Conditions.ActorTargetDistance()
            {
                comparison = comparisonType,
                minParam = NodeHelper.GetInputPortParam<HTNParamFP, FP>(GetInputPortByName(inParamMin)),
                maxParam = NodeHelper.GetInputPortParam<HTNParamFP, FP>(GetInputPortByName(inParamMax)),
            };
        }
    }
}
#endif