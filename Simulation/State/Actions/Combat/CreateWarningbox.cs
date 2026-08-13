using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class CreateWarningbox : HNSFStateAction
    {
        public int identifier;
        public int priority;
        public AssetRef<HitInfoBase> hitInfo;
        public bool isThrow;
        public int activeAtOffset;
        public int inactiveAtOffset;
        public byte dangerLevel;
        public byte responseDifficulty;

        public bool useExternalShapeConfig;
        [DrawIf(nameof(useExternalShapeConfig), true)]
        public AssetRef<Shape3DConfigOffsetRotation> externalShapeConfigReference;
        [DrawIf(nameof(useExternalShapeConfig), false)]
        public FPVector3 offset;
        [DrawIf(nameof(useExternalShapeConfig), false)]
        public FPVector3 rotation;
        [DrawIf(nameof(useExternalShapeConfig), false)]
        public Shape3DConfig shapeConfig = new();
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BoxCombatant>(entity, out var boxCombatant)
                || !frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform)) return false;
            
            var boxList = frame.ResolveList(boxCombatant->warningboxList);

            if (boxCombatant->WarningboxExistWithId(frame, identifier))
            {
                Log.Debug($"Warningbox of id {identifier} already exist on entity {entity.ToString()}. Error came from state {frame.FindAsset<HNSFState>(stateContext.workingState).Label}");
                return false;
            }

            Shape3D shape;
            FPVector3 realOffset;
            FPVector3 realRotation;
            if (useExternalShapeConfig && frame.TryFindAsset(externalShapeConfigReference, out var externalShape2DConfig))
            {
                shape = externalShape2DConfig.shape.CreateShape(frame);
                realOffset = externalShape2DConfig.offset;
                realRotation = externalShape2DConfig.rotation;
            }
            else
            {
                shape = shapeConfig.CreateShape(frame);
                realOffset = offset;
                realRotation = rotation;
            }
            
            var boxEntity = frame.Create();
            
            var physicsCollider = new PhysicsCollider3D
            {
                Layer = frame.Layers.GetLayerIndex(HnSFConstants.Layer_Warningbox),
                IsTrigger = true,
                Shape = shape
            };

            frame.Add(boxEntity, new Warningbox()
            {
                active = true, 
                owner = entity, 
                id = identifier,
                activeAt = frame.Number + activeAtOffset,
                inactiveAt = frame.Number + inactiveAtOffset,
                isThrow = isThrow,
                damageSourceInfoRef = hitInfo.Id,
                dangerLevel = dangerLevel,
                responseDifficulty = responseDifficulty
            });
            frame.Add(boxEntity, new Transform3D(){ Position = transform->Position + transform->TransformDirection(realOffset), Rotation = FPQuaternion.Euler(transform->EulerAngles + realRotation)});
            frame.Add(boxEntity, new Parented3D() { parent = entity, localOffset = realOffset, localEuler = realRotation });
            frame.Add(boxEntity, physicsCollider);
            
            boxList.Add(boxEntity);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new CreateWarningbox());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as CreateWarningbox;
            t.identifier = identifier;
            t.priority = priority;
            t.hitInfo = hitInfo;
            t.isThrow = isThrow;
            t.dangerLevel = dangerLevel;
            t.responseDifficulty = responseDifficulty;
            t.activeAtOffset = activeAtOffset;
            t.inactiveAtOffset = inactiveAtOffset;
            t.useExternalShapeConfig = useExternalShapeConfig;
            t.externalShapeConfigReference = externalShapeConfigReference;
            t.offset = offset;
            t.rotation = rotation;
            t.shapeConfig = new Shape3DConfig()
            {
                BoxExtents = shapeConfig.BoxExtents,
                CapsuleHeight = shapeConfig.CapsuleHeight,
                CapsuleRadius = shapeConfig.CapsuleRadius,
                SphereRadius = shapeConfig.SphereRadius,
                CompoundShapes = shapeConfig.CompoundShapes.ToArray(),
                IsPersistent = shapeConfig.IsPersistent,
                PositionOffset = shapeConfig.PositionOffset,
                RotationOffset = shapeConfig.RotationOffset,
                ShapeType = shapeConfig.ShapeType,
                UserTag = shapeConfig.UserTag
            };
            return base.CopyTo(target);
        }
    }
}
