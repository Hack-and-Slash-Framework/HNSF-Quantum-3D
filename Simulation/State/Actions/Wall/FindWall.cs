using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Wall/Find Wall")]
    public unsafe partial class FindWall : HNSFStateAction
    {
        public enum DirType
        {
            FacingDirection,
            MovingInput,
            MovementDirection,
        }
        
        public bool clearWallInfo;
        public LayerMaskParam raycastMask;
        public FPVector3 offset;
        public bool addKccRadiusToDistance = true;
        public FP raycastDistance = 1;
        public DirType direction;
        public FP validWallAngle = 20;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)
                || !frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc)) return false;
            if (clearWallInfo) frame.Remove<GotWallInfo>(entity);

            var inputDir = FPVector3.Zero;

            switch (direction)
            {
                case DirType.FacingDirection:
                    inputDir = transform->Forward;
                    break;
                case DirType.MovingInput:
                    var bufferCam = frame.Unsafe.GetPointer<ActorInputCamera>(entity);
                    var bufferMovement = frame.Unsafe.GetPointer<ActorInputBufferMovement>(entity);
                    inputDir = bufferCam->GetMovementVector(0, bufferMovement->GetMovement(0), true);
                    break;
                case DirType.MovementDirection:
                    if (frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var bap))
                    {
                        inputDir = bap->GetKinematicHorizontalSpeed(frame, entity);
                    }
                    break;
            }

            if (inputDir == FPVector3.Zero) return false;
            inputDir.Y = 0;
            inputDir = inputDir.Normalized;

            var dist = raycastDistance;

            if (addKccRadiusToDistance && frame.TryFindAsset(kcc->Settings, out var kccSettings))
            {
                dist += kccSettings.Radius;
            }
            
            var raycastHit = frame.Physics3D.Raycast(
                transform->Position + kcc->Data.PositionOffset + (new FPVector3(0, kcc->Data.Height / FP._2, 0)),
                inputDir,
                dist,
                raycastMask.Get(frame),
                options: QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo
                );

            if (raycastHit.HasValue
                && FPVector3.Angle(-inputDir, raycastHit.Value.Normal) <= validWallAngle)
            {
                frame.AddOrGet<GotWallInfo>(entity, out var gwi);
                gwi->wallPoint = raycastHit.Value.Point;
                gwi->wallNormal = raycastHit.Value.Normal;
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