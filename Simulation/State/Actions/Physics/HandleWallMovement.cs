using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Handle Wall Movement")]
    public unsafe partial class HandleWallMovement : HNSFStateAction
    {
        public enum SurfaceType
        {
            Raw,
            Ground,
            Wall
        }

        public AssetRef<ExternalLayerMask> levelLayerMaskAssetRef;
        public SurfaceType rotationBasedOn = SurfaceType.Raw;

        public AssetRef<Tag> stateRun;
        public AssetRef<Tag> stateFall;

        public FP snapToGroundDist = FP._0_05;
        public FP wrapAroundFudge = FP._0_10;
        public FP sideFudge = FP._0_10;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(entity);
            var gotWallInfo = frame.Unsafe.TryGetPointer<GotWallInfo>(entity, out var gwi);
            KCC* kcc = frame.Unsafe.GetPointer<KCC>(entity);
            var kccSettings = frame.FindAsset(kcc->Settings);
            var levelLayerMask = frame.FindAsset(levelLayerMaskAssetRef).mask;
            var centerPosition =
                transform->Position +
                transform->TransformDirection(kcc->Data.PositionOffset +
                                              new FPVector3(0, (kcc->Data.Height / FP._2), 0));
            FPVector3 midPoint = transform->TransformDirection(kcc->Data.PositionOffset + new FPVector3(0, (kcc->Data.Height / FP._2), 0));
            var halfWidth = kccSettings.Radius;

            var translation = physics->force * frame.DeltaTime;

            if (!gotWallInfo)
            {
                var ffRaycast = frame.Physics3D.Raycast(
                    origin: centerPosition,
                    direction: transform->Forward,
                    distance: halfWidth + wrapAroundFudge,
                    layerMask: levelLayerMask,
                    options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);
                
                RedGreenDrawRay(centerPosition, transform->Forward * (halfWidth + wrapAroundFudge), ffRaycast.HasValue);

                if (ffRaycast.HasValue == false)
                {
                    var flRaycast = frame.Physics3D.Raycast(
                        origin: centerPosition + (transform->Forward * ((halfWidth * FP._2) + wrapAroundFudge)),
                        direction: transform->Left,
                        distance: halfWidth,
                        layerMask: levelLayerMask,
                        options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);
                    RedGreenDrawRay(centerPosition + (transform->Forward * (halfWidth + wrapAroundFudge)),
                        transform->Left * halfWidth, flRaycast.HasValue);

                    if (flRaycast.HasValue)
                    {
                        FPVector3 newPosition =
                            flRaycast.Value.Point
                            + (flRaycast.Value.Normal * kccSettings.Radius)
                            - midPoint;
                        
                        transform->Rotation = FPQuaternion.LookRotation(-flRaycast.Value.Normal);
                        transform->Position = newPosition;
                        physics->SetOverallVelocity(frame, entity, FPVector3.Zero);
                        return false;
                    }

                    var frRaycast = frame.Physics3D.Raycast(
                        origin: centerPosition + (transform->Forward * ((halfWidth * FP._2) + wrapAroundFudge)),
                        direction: transform->Right,
                        distance: halfWidth,
                        layerMask: levelLayerMask,
                        options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

                    if (frRaycast.HasValue)
                    {
                        FPVector3 newPosition =
                            frRaycast.Value.Point
                            + (frRaycast.Value.Normal * kccSettings.Radius)
                            - midPoint;
                        
                        transform->Rotation = FPQuaternion.LookRotation(-frRaycast.Value.Normal);
                        transform->Position = newPosition;
                        physics->SetOverallVelocity(frame, entity, FPVector3.Zero);
                        return false;
                    }
                    
                    var fdRaycast = frame.Physics3D.Raycast(
                        origin: centerPosition + (transform->Forward * ((halfWidth * FP._2) + wrapAroundFudge)),
                        direction: transform->Down,
                        distance: halfWidth,
                        layerMask: levelLayerMask,
                        options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

                    if (fdRaycast.HasValue)
                    {
                        transform->Rotation = FPQuaternion.Euler(0, 0, 0);
                        transform->Position = fdRaycast.Value.Point;
                        physics->SetOverallVelocity(frame, entity, FPVector3.Zero);
                        return false;
                    }
                }

                ChangeState(frame, entity, stateFall, ref stateContext);
                return false;
            }
            
            var shape3d = Shape3D.CreateCapsule(
                radius: kccSettings.Radius,
                extent: kcc->Data.Height / FP._2,
                posOffset: null,
                rotOffset: null);

            var sweep = frame.Physics3D.ShapeCastAll(
                start: transform->Position,
                rotation: transform->Rotation,
                shape: shape3d,
                translation: translation,
                layerMask: levelLayerMask,
                options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

            var wallTranslation = FPVector3.ProjectOnPlane(translation, gwi->wallNormal);

            var localizedTranslation = wallTranslation.InverseTransformDirection(FPQuaternion.LookRotation(gwi->wallNormal, transform->Up));
            
            var floorRaycast = frame.Physics3D.Raycast(
                origin: centerPosition,
                direction: transform->Down,
                distance: (kcc->Data.Height / FP._2) + snapToGroundDist,
                layerMask: levelLayerMask,
                options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

            // LAND ON FLOOR
            if (wallTranslation.SqrMagnitude > 0
                && FPVector3.Dot(transform->Down, wallTranslation) > FP._0
                && floorRaycast.HasValue)
            {
                transform->Position = floorRaycast.Value.Point;

                ChangeState(frame, entity, stateRun, ref stateContext);
                return false;
            }

            var ceilingRaycast = frame.Physics3D.Raycast(
                origin: centerPosition,
                direction: transform->Up,
                distance: (kcc->Data.Height / FP._2),
                layerMask: levelLayerMask,
                options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

            if (ceilingRaycast.HasValue)
            {
                if (localizedTranslation.Y > 0) localizedTranslation.Y = 0;
            }

            // CHANGE WALL - RIGHT
            var rightRaycast = frame.Physics3D.Raycast(
                origin: centerPosition,
                direction: transform->Right,
                distance: halfWidth + sideFudge,
                layerMask: levelLayerMask,
                options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);
            
            //RedGreenDrawRay(centerPosition, transform->Right * (halfWidth + sideFudge), rightRaycast.HasValue);

            if (rightRaycast.HasValue)
            {
                if (localizedTranslation.X < 0)
                {
                    FPVector3 newPosition =
                        rightRaycast.Value.Point
                        + (rightRaycast.Value.Normal * kccSettings.Radius)
                        - midPoint;
                        
                    transform->Rotation = FPQuaternion.LookRotation(-rightRaycast.Value.Normal);
                    transform->Position = newPosition;
                    physics->SetOverallVelocity(frame, entity, localizedTranslation.TransformDirection(FPQuaternion.LookRotation(rightRaycast.Value.Normal, transform->Up)));
                    return false;
                }
            }
            
            var leftRaycast = frame.Physics3D.Raycast(
                origin: centerPosition,
                direction: transform->Left,
                distance: halfWidth + sideFudge,
                layerMask: levelLayerMask,
                options: QueryOptions.HitSolids | QueryOptions.ComputeDetailedInfo);

            //RedGreenDrawRay(centerPosition, transform->Left * (halfWidth + sideFudge), leftRaycast.HasValue);
            
            if (leftRaycast.HasValue)
            {
                if (localizedTranslation.X > 0)
                {
                    FPVector3 newPosition =
                        leftRaycast.Value.Point
                        + (leftRaycast.Value.Normal * kccSettings.Radius)
                        - midPoint;
                        
                    transform->Rotation = FPQuaternion.LookRotation(-leftRaycast.Value.Normal);
                    transform->Position = newPosition;
                    physics->SetOverallVelocity(frame, entity, localizedTranslation.TransformDirection(FPQuaternion.LookRotation(leftRaycast.Value.Normal, transform->Up)));
                    return false;
                }
            }
            
            transform->Position += localizedTranslation.TransformDirection(FPQuaternion.LookRotation(gwi->wallNormal, transform->Up));
            return false;
        }

        public void RedGreenDrawRay(FPVector3 origin, FPVector3 direction, bool hasValue)
        {
            if(hasValue) Draw.Ray(origin, direction, ColorRGBA.Green);
            else Draw.Ray(origin, direction, ColorRGBA.Red);
        }

        void ChangeState(Frame frame, EntityRef entity, AssetRef<Tag> stateTag, ref HNSFStateContext stateContext)
        {
            if (frame.Unsafe.TryGetPointer<GenericStateMachine>(entity, out var csm)
                && frame.TryFindAsset(csm->stateAgent.stateSet, out var stateSet)
                && frame.TryFindAsset(csm->stateAgent.stateData.state, out var currentState)
                && stateSet.AttemptGetStateByTag(csm->stateAgent.stateData.moveset, stateTag, out var toStateRef))
            {
                stateContext.agentData->toStateRequested = true;
                stateContext.agentData->toState = toStateRef;
                stateContext.agentData->toFrame = 0;
            }
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new HandleWallMovement());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as HandleWallMovement;
            return base.CopyTo(target);
        }
    }
}