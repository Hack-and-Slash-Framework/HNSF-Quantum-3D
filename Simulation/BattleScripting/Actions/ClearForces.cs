using System;
using HnSF.core.GroupControl.Actions;
using HnSF.core.GroupControl.Functions;
using HnSF.Nodes;
using Photon.Deterministic;
using Quantum;
#if QUANTUM_UNITY
using UnityEngine.Scripting.APIUpdating;
#endif
#if UNITY_EDITOR
using HnSF.core.GroupControl.Nodes;
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.GroupControl.Actions
{
    [Serializable]
    public unsafe partial class ClearForces : GroupControlAction
    {
        public GroupControlFunctionEntityRef[] entityRefFunctions = Array.Empty<GroupControlFunctionEntityRef>();
        
        public override void OnEnter(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
        }
        
        public override bool Tick(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            foreach (var entityRefFunction in entityRefFunctions)
            {
                var targetEntity = entityRefFunction.Execute(frame, infoEntityRef, ref context);
                if(targetEntity == EntityRef.None) continue;
                if (frame.Unsafe.TryGetPointer<BattleActorPhysics>(targetEntity, out var physics))
                {
                    physics->force = FPVector3.Zero;
                    physics->SetExternalImpulse(frame, targetEntity, FPVector3.Zero);
                    physics->SetOverallVelocity(frame, targetEntity, FPVector3.Zero);
                }
            }
            return true;
        }
        
        public override void OnExit(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.GroupControl.Nodes
{
    [Serializable]
    [UseWithGraph(typeof(ActorGroupScriptGraph))]
    internal class ClearForces : ActorGroupControlNode
    {
        public const string inPortEntityRefFunctions = "EntityRefFunctions";
        
        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);

            context.AddInputPort(inPortEntityRefFunctions)
                .WithDisplayName("Entity Ref Functions")
                .Build();
        }

        public override GroupControlAction Convert()
        {
            var targetTag = NodeHelper.GetInputPortValue<Tag>(this.GetInputPortByName(inPortEntityRefFunctions));
            var functionList = ConvertFunctionNodes<GroupControlFunctionEntityRef>(GetInputPortByName(inPortEntityRefFunctions));
            
            return new Actions.ClearForces()
            {
                entityRefFunctions = functionList.ToArray()
            };
        }
    }
}
#endif