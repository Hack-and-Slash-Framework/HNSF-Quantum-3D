using System;
using HnSF.core.AI.HTN.Conditions;
using Quantum;
#if UNITY_EDITOR
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.AI.HTN.Conditions
{
    [Serializable]
    public unsafe partial class ActorStateGroundedType : ICondition
    {
        public string Label
        {
            get => label;
            set => label = value;
        }

        public string label;

        public StateGroundedType state = StateGroundedType.GROUNDED;

        public bool IsValid(ref HTNAgentContext context)
        {
            var frame = context.frame;
            
            if (!frame.Unsafe.TryGetPointer<BattleActorAI>(context.agentEntityRef, out var battleActorAI)) return false;

            if (frame.Unsafe.TryGetPointer<BattleActorPhysics>(battleActorAI->aiActorRef, out var physics))
            {
                return physics->currentGroundedState == state;
            }
            return false;
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.AI.HTN.Nodes
{
    [Serializable]
    [UseWithGraph(typeof(PrimitiveTaskGraph), typeof(HTNDomainGraph))]
    public unsafe class ConditionActorStateGroundedType : ConditionBase
    {
        public const string OPTION_STATE = "State";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
            context.AddOption<StateGroundedType>(OPTION_STATE)
                .WithDisplayName("State")
                .WithDefaultValue(StateGroundedType.GROUNDED)
                .Build();
        }

        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            //AddInputOutputExecutionPorts(context);
        }

        public override ICondition Convert()
        {
            //this.GetNodeOptionByName(OPTION_LABEL).TryGetValue<string>(out var label);
            this.GetNodeOptionByName(OPTION_STATE).TryGetValue<StateGroundedType>(out var state);

            return new Conditions.ActorStateGroundedType()
            {
                //Label = label,
                state = state,
            };
        }
    }
}
#endif