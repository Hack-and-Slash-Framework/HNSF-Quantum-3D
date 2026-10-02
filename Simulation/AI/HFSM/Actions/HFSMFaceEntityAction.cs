using System;
using Photon.Deterministic;

namespace Quantum
{
    [Serializable]
    public unsafe partial class HFSMFaceEntityAction : ExecuteIfConditionAction
    {
        public AIParamEntityRef entityRef;
        
        public override void ConditionalExecute(Frame frame, EntityRef entity, ref AIContext aiContext, AIContextUser* user,
            AIConfig aiConfig)
        {
            var facetoEntityRef = entityRef.Resolve(frame,entity, user->Blackboard, aiConfig);
            
            if(!frame.Unsafe.TryGetPointer<Transform3D>(facetoEntityRef, out var facingTransform3D)) return;

            if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
            {
                kcc->SetLookRotation(FPQuaternion.LookRotation((facingTransform3D->Position - kcc->Position).XOZ.Normalized));
            }
            
            if (frame.Unsafe.TryGetPointer<Transform3D>(entity, out var selfTransform3D))
            {
                selfTransform3D->Rotation = FPQuaternion.LookRotation((facingTransform3D->Position - kcc->Position).XOZ.Normalized);
            }
        }
    }
}