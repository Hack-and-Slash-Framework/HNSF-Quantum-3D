using System;
using Quantum;
using HnSF.core.AI.HTN.Conditions;
using Quantum.Physics3D;
#if QUANTUM_UNITY
using UnityEngine.Scripting.APIUpdating;
#endif
#if UNITY_EDITOR
using HnSF.core.GroupControl.Nodes;
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.AI.HTN.Conditions
{
    [Serializable]
    public unsafe partial class ConditionActorInDanger : ICondition
    {
        public bool inverse;
        public string Label { get; set; }
        
        public bool IsValid(ref HTNAgentContext context)
        {
            var frame = context.frame;
            
            if (!frame.Unsafe.TryGetPointer<BattleActorAI>(context.agentEntityRef, out var battleActorAI)) return false;
            if(!frame.Exists(battleActorAI->target)) return false;

            var targetTransform = frame.Unsafe.GetPointer<Transform3D>(battleActorAI->target);
            
            if (frame.Unsafe.TryGetPointer<PhysicsCollider3D>(battleActorAI->target, out var physicsCollider2D))
            {
                HitCollection3D hc = frame.Physics3D.OverlapShape(
                    targetTransform->Position,
                    targetTransform->Rotation,
                    Shape3D.CreateSphere(physicsCollider2D->Shape.BroadRadius),
                    layerMask: frame.SimulationConfig.layerMaskWarningbox,
                    options: QueryOptions.HitAll);

                var selfHasCombatTeam = frame.Unsafe.TryGetPointer<CombatTeam>(battleActorAI->target, out var selfCombatTeam);
                
                for (int i = 0; i < hc.Count; i++)
                {
                    var wb = frame.Unsafe.GetPointer<Warningbox>(hc[i].Entity);
                    if(wb->owner == battleActorAI->target) continue;
                    if (!frame.Exists(wb->owner)) return true;
                    if (!frame.Unsafe.TryGetPointer<CombatTeam>(wb->owner, out var otherCombatTeam)) return true;
                    if (selfHasCombatTeam && otherCombatTeam->IsHostileTowards(frame, selfCombatTeam)) return true;
                }
            }

            return false;
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.AI.HTN.Nodes
{
    [Serializable]
    //[UseWithGraph(typeof(PrimitiveTaskGraph))]
    internal class ConditionActorIsInDangerNode : ConditionBase
    {
        public const string optionInverse = "Inverse";
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<bool>(optionInverse)
                .WithDisplayName("Inverse?")
                .WithDefaultValue(false)
                .Build();
        }

        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            //AddInputOutputExecutionPorts(context);
        }

        public override ICondition Convert()
        {
            //this.GetNodeOptionByName(OPTION_LABEL).TryGetValue<string>(out var label);
            GetNodeOptionByName(optionInverse).TryGetValue(out bool inverse);
            
            return new Conditions.ConditionActorInDanger()
            {
                //Label = label
                inverse = inverse
            };
        }
    }
}
#endif