using System;
using HnSF.core.GroupControl.Functions;
using Photon.Deterministic;
using Quantum;
#if QUANTUM_UNITY
using UnityEngine.Scripting.APIUpdating;
#endif
#if UNITY_EDITOR
using HnSF.core.GroupControl.Nodes;
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.GroupControl.Grabbers
{
    [Serializable]
    public unsafe partial class DotBetweenEntities : GroupControlRule
    {
        public bool inverse;
        public GroupControlFunctionEntityRef entityAFunct;
        public GroupControlFunctionEntityRef entityBFunct;

        public ComparisonType comparison;
        public FP min;
        public FP max;
        
        public override bool IsValid(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            var entityA = entityAFunct.Execute(frame, infoEntityRef, ref context);
            var entityB = entityBFunct.Execute(frame, infoEntityRef, ref context);
            
            if(!frame.Unsafe.TryGetPointer<Transform3D>(entityA, out var transformA)
               || !frame.Unsafe.TryGetPointer<Transform3D>(entityB, out var transformB)) return false;

            var val = FPVector3.Dot(transformA->Position, transformB->Position);
            bool result = false;
            
            switch (comparison)
            {
                case ComparisonType.Inbetween:
                    result = val >= min && val <= max;
                    break;
                case ComparisonType.Equals:
                    result = val == min;
                    break;
                case ComparisonType.MoreThan:
                    result = val > min;
                    break;
                case ComparisonType.MoreThanOrEqualTo:
                    result = val >= min;
                    break;
                case ComparisonType.LessThan:
                    result = val < min;
                    break;
                case ComparisonType.LessThanOrEqualTo:
                    result = val <= min;
                    break;
            }
            
            if (inverse) result = !result;
            return result;
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.GroupControl.Grabbers
{
    [Serializable]
    internal class ConditionDotBetweenEntities : RuleNodeBase
    {
        public const string OPTION_COMPARISONTYPE = "ComparisonType";
        public const string OPTION_COMPARETO_MIN = "CompareToMin";
        public const string OPTION_COMPARETO_MAX = "CompareToMax";
        public const string inEntityAFunct = "EntityAFunct";
        public const string inEntityBFunct = "EntityBFunct";
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<ComparisonType>(OPTION_COMPARISONTYPE)
                .WithDefaultValue(ComparisonType.Inbetween)
                .Build();
            
            context.AddOption<FP>(OPTION_COMPARETO_MIN)
                .WithDefaultValue(0)
                .Build();
            
            context.AddOption<FP>(OPTION_COMPARETO_MAX)
                .WithDefaultValue(0)
                .Build();
        }

        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);

            context.AddInputPort(inEntityAFunct)
                .WithDisplayName("Entity A Function")
                .Build();

            context.AddInputPort(inEntityBFunct)
                .WithDisplayName("Entity B Function")
                .Build();
        }

        public override GroupControlRule Convert()
        {
            this.GetNodeOptionByName(OPTION_LABEL).TryGetValue<string>(out var label);
            this.GetNodeOptionByName(OPTION_COMPARISONTYPE).TryGetValue<ComparisonType>(out var comparison);
            this.GetNodeOptionByName(OPTION_COMPARETO_MIN).TryGetValue<FP>(out var min);
            this.GetNodeOptionByName(OPTION_COMPARETO_MAX).TryGetValue<FP>(out var max);
            
            return new Grabbers.DotBetweenEntities()
            {
                Label = label,
                comparison = comparison,
                min = min,
                max = max,
                entityAFunct = ConvertFunctionNode<GroupControlFunctionEntityRef>(GetInputPortByName(inEntityAFunct)),
                entityBFunct = ConvertFunctionNode<GroupControlFunctionEntityRef>(GetInputPortByName(inEntityBFunct)),
            };
        }
    }
}
#endif