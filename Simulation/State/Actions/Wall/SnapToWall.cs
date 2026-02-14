using Photon.Deterministic;
using System;
using Quantum;
using UnityEngine.Serialization;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Wall/Snap To Wall")]
    public unsafe partial class SnapToWall : HNSFStateAction
    {
        public enum ModifyType
        {
            SET,
            MoveTowards
        }
        
        public ModifyType modifyType;
        public FP moveSpeed = 1;
        public FP fudging = FP._0_10;
        
        public bool setRotationToWallRotation;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)
                || !frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc)
                || !frame.Unsafe.TryGetPointer<GotWallInfo>(entity, out var gwi)
                || !frame.TryFindAsset(kcc->Settings, out var kccSettings)) return false;
            
            var adjustedNormal = gwi->wallNormal;

            if(setRotationToWallRotation) transform->Rotation = FPQuaternion.LookRotation(-gwi->wallNormal, FPVector3.ProjectOnPlane(transform->Up, gwi->wallNormal));
            
            FPVector3 midPoint = transform->TransformDirection(kcc->Data.PositionOffset + new FPVector3(0, (kcc->Data.Height / FP._2), 0));
            
            FPVector3 newPosition =
                gwi->wallPoint
                + (adjustedNormal * kccSettings.Radius)
                - midPoint;
            
            if (FPVector3.DistanceSquared(transform->Position, newPosition) <= fudging)
            {
                return false;
            }
            
            switch (modifyType)
            {
                case ModifyType.SET:
                    transform->Position = newPosition;
                    break;
                case ModifyType.MoveTowards:
                    transform->Position = FPVector3.MoveTowards(transform->Position, newPosition, moveSpeed * frame.DeltaTime);
                    break;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new FindWall());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as FindWall;
            
            return base.CopyTo(target);
        }
    }
}