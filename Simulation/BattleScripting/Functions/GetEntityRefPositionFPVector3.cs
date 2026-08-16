using System;
using HnSF.core.GroupControl.Functions;
using Photon.Deterministic;
using Quantum;
#if QUANTUM_UNITY
using UnityEngine;
#endif
#if UNITY_EDITOR
using HnSF.Nodes;
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.GroupControl.Functions
{
    [Serializable]
    public unsafe partial class GetEntityRefPositionFPVector3 : GroupControlFunctionFPVector3
    {
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionEntityRef functionEntityRef;
        public BattleScriptingParamFPVector3 paramOffset;
        
        public override FPVector3 Execute(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            var entityRef = functionEntityRef.Execute(frame, infoEntityRef, ref context);
            if (!frame.Exists(entityRef)
                || !frame.Unsafe.TryGetPointer<Transform3D>(entityRef, out var transform)) return FPVector3.Zero;
            return transform->Position + transform->TransformDirection(paramOffset.Resolve(frame, infoEntityRef, ref context));
        }
    }
}

# if UNITY_EDITOR
namespace HnSF.core.GroupControl.Nodes
{
    [Serializable]
    [UseWithGraph(typeof(ActorGroupScriptGraph))]
    internal class GetEntityRefPositionFPVector2 : FunctionNodeBase
    {
        public const string inEntityRefFunction = "EntityRefFunction";
        public const string inOffset = "Offset";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);
        }

        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);
            
            context.AddInputPort(inEntityRefFunction)
                .WithDisplayName("Entity Ref Function")
                .Build();
            
            context.AddInputPort<FPVector3>(inOffset)
                .WithDisplayName("Offset")
                .Build();
        }

        public override GroupControlFunction Convert()
        {
            return new HnSF.core.GroupControl.Functions.GetEntityRefPositionFPVector3()
            {
                functionEntityRef = ConvertFunctionNode<GroupControlFunctionEntityRef>(GetInputPortByName(inEntityRefFunction)),
                paramOffset = GetInputPortParam<BattleScriptingParamFPVector3, FPVector3>(GetInputPortByName(inOffset))
            };
        }
    }
}
#endif