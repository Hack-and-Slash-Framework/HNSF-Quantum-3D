using System;
using HnSF.core.GroupControl.Actions;
using HnSF.core.GroupControl.Functions;
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
    public unsafe partial class PlayOneShotSFX : GroupControlAction
    {
        public AssetRef<ExternalPlaySoundRequest> sfxExternalRequest;
#if QUANTUM_UNITY
        [SerializeReference, SubclassSelector]
#endif
        public GroupControlFunctionFPVector3 playPosition;
        
        public override void OnEnter(Frame frame, EntityRef infoEntityRef, ref BattleScriptContext context)
        {
            if (!frame.TryFindAsset(sfxExternalRequest, out var externalRequestAsset))
            {
                Log.Debug("Could not find asset: " + sfxExternalRequest);
                return;
            }
            var request = externalRequestAsset.request;
            var sfx = request.GetRngSound(frame.RNG);
            var pos = playPosition.Execute(frame, infoEntityRef, ref context);

            SoundEffectHelper.PlaySound(frame, request, sfx, infoEntityRef, pos, isGlobal: true);
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
    internal class PlayOneShotSFXNode : ActorGroupControlNode
    {
        public const string inVisualEffectRequestParam = "VisualEffectRequest";
        public const string InPort_PositionFunction = "Position";
        
        protected override void OnDefinePorts(Node.IPortDefinitionContext context)
        {
            AddInputOutputExecutionPorts(context);
            
            context.AddInputPort(InPort_PositionFunction)
                .WithDisplayName("FPVector3 Position Function")
                .WithConnectorUI(PortConnectorUI.Circle)
                .Build();
            
            context.AddInputPort<AssetRef<ExternalPlaySoundRequest>>(inVisualEffectRequestParam)
                .WithDisplayName("SFX Request")
                .Build();
        }

        public override GroupControlAction Convert()
        {
            return new Actions.PlayOneShotSFX()
            {
                sfxExternalRequest = NodeHelper.GetInputPortValue<AssetRef<ExternalPlaySoundRequest>>(GetInputPortByName(inVisualEffectRequestParam)),
                playPosition = ConvertFunctionNode<GroupControlFunctionFPVector3>(GetInputPortByName(InPort_PositionFunction))
            };
        }
    }
}
#endif