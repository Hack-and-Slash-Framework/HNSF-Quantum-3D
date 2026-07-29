using System;
using HnSF.core.GroupControl.Actions;
using HnSF.core.GroupControl.Functions;
using Photon.Deterministic;
using Quantum;
#if QUANTUM_UNITY
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#endif
#if UNITY_EDITOR
using HnSF.core.GroupControl.Nodes;
using Unity.GraphToolkit.Editor;
#endif

namespace HnSF.core.GroupControl.Actions
{
    [Serializable]
    public unsafe partial class SetEntityPosition : GroupControlAction
    {
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionEntityRef entityGrabberFunction;
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionFPVector3 position;

        public bool asTeleport;
        
        public override void OnEnter(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            var otherEntityRef = entityGrabberFunction.Execute(frame, infoEntityRef, ref context);
            if (!frame.Exists(otherEntityRef)) return;
            var transform3D = frame.Unsafe.GetPointer<Transform3D>(otherEntityRef);
            transform3D->Position = position.Execute(frame, infoEntityRef, ref context);
        }
        
        public override bool Tick(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
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
    internal class SetEntityPosition : ActorGroupControlNode
    {
        //public const string OPTION_CAMERA_TAG = "CameraTag";
        public const string PORT_GRABBER_FUNCTION = "EntityGrabber";
        public const string PORT_POSITION = "Position";
        public const string optionTeleport = "Teleport";
        
        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            base.OnDefineOptions(context);

            context.AddOption<bool>(optionTeleport)
                .WithDisplayName("As Teleport?")
                .Build();
        }

        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);
            
            context.AddInputPort(PORT_GRABBER_FUNCTION)
                .WithDisplayName("Entity Ref Function")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
            
            context.AddInputPort(PORT_POSITION)
                .WithDisplayName("Position")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
        }

        public override GroupControlAction Convert()
        {
            GroupControlFunctionEntityRef function = null;
            GroupControlFunctionFPVector3 position = null;
            var portEntityRef = GetInputPortByName(PORT_GRABBER_FUNCTION).FirstConnectedPort;
            var portPosition = GetInputPortByName(PORT_POSITION).FirstConnectedPort;
            GetNodeOptionByName(optionTeleport).TryGetValue(out bool asTeleport);

            if (portEntityRef.GetNode() is FunctionNodeBase fnEntityRef)
            {
                function = fnEntityRef.Convert() as GroupControlFunctionEntityRef;
            }

            if (portPosition.GetNode() is FunctionNodeBase fnPosition)
            {
                position = fnPosition.Convert() as GroupControlFunctionFPVector3;
            }
            
            return new Actions.SetEntityPosition()
            {
                entityGrabberFunction = function,
                position = position,
                asTeleport = asTeleport
            };
        }
    }
}
#endif