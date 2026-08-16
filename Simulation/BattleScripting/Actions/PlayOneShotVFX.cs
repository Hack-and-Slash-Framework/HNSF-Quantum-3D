using System;
using HnSF.core.GroupControl.Actions;
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

namespace HnSF.core.GroupControl.Actions
{
    [Serializable]
    public unsafe partial class PlayOneShotVFX : GroupControlAction
    {
        public AssetRef<ExternalPlayVisualEffectRequest> vfxExternalRequest;
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionFPVector3 playPosition;
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionFPVector3 playRotation;
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionFPVector3 playScale;
        
        public override void OnEnter(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            if (!frame.TryFindAsset(vfxExternalRequest, out var externalRequestAsset))
            {
                Log.Debug("Could not find asset: " + vfxExternalRequest);
                return;
            }
            var request = externalRequestAsset.request;
            var vfx = request.GetRngVFX(frame.RNG);
            var pos = playPosition.Execute(frame, infoEntityRef, ref context);
            var rot = playRotation.Execute(frame, infoEntityRef, ref context);
            var scale = playScale.Execute(frame, infoEntityRef, ref context);
            
            VisualEffectHelper.PlayVisualEffect(frame, request, vfx, infoEntityRef, pos, rot,
                scale, FPVector3.Zero, false);
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
    internal class PlayOneShotVFXNode : ActorGroupControlNode
    {
        public const string inVisualEffectRequestParam = "VisualEffectRequestParam";
        public const string inPositionFunction = "PositionFunction";
        public const string inRotationFunction = "RotationFunction";
        public const string inScaleFunction = "ScaleFunction";
        
        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);
            
            context.AddInputPort(inPositionFunction)
                .WithDisplayName("FPVector3 Position Function")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
            
            context.AddInputPort(inRotationFunction)
                .WithDisplayName("FPVector3 Rotation Function")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
            
            context.AddInputPort(inScaleFunction)
                .WithDisplayName("FPVector3 Scale Function")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
            
            context.AddInputPort<AssetRef<ExternalPlayVisualEffectRequest>>(inVisualEffectRequestParam)
                .WithDisplayName("Visual Effect Request")
                .Build();
        }

        public override GroupControlAction Convert()
        {
            return new Actions.PlayOneShotVFX()
            {
                vfxExternalRequest = NodeHelper.GetInputPortValue<AssetRef<ExternalPlayVisualEffectRequest>>(GetInputPortByName(inVisualEffectRequestParam)),
                playPosition = ConvertFunctionNode<GroupControlFunctionFPVector3>(GetInputPortByName(inPositionFunction)),
                playRotation = ConvertFunctionNode<GroupControlFunctionFPVector3>(GetInputPortByName(inRotationFunction)),
                playScale = ConvertFunctionNode<GroupControlFunctionFPVector3>(GetInputPortByName(inScaleFunction)),
            };
        }
    }
}
#endif