using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class CreateAndDestroyCollisionbox : HNSFStateAction
    {
        public bool autoDelete = true;
        public int collisionboxIdentifier;
        public int priority;
        
        public bool useExternalShapeConfig;
        [DrawIf(nameof(useExternalShapeConfig), true)]
        public AssetRef<Shape3DConfigOffsetRotation> externalShape2DConfigReference;
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

            if (autoDelete && rangePercent >= 1)
            {
                boxCombatant->DeleteCollisionboxByID(frame, collisionboxIdentifier);
                return false;
            }
            if (boxCombatant->CollisionboxExistWithId(frame, collisionboxIdentifier)) return false;
            
            
            var boxList = frame.ResolveList(boxCombatant->collisionboxList);
            Shape3D shape;
            FPVector3 realOffset;
            FPVector3 realRotation;
            if (useExternalShapeConfig && frame.TryFindAsset(externalShape2DConfigReference, out var externalShape2DConfig))
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
                
            var boxPhysicsCollider = new PhysicsCollider3D
            {
                Layer = frame.Layers.GetLayerIndex(HnSFConstants.Layer_Collisionbox),
                IsTrigger = true,
                Shape = shape
            };
                
            var boxEntity = frame.Create();
            frame.Add(boxEntity, new Collisionbox() { active = true, owner = entity, id = collisionboxIdentifier});
            frame.Add(boxEntity, new Transform3D(){ Position = transform->Position + transform->TransformDirection(realOffset), Rotation = FPQuaternion.Euler(transform->EulerAngles + realRotation)});
            frame.Add(boxEntity, new Parented3D() { parent = entity, localOffset = realOffset, localEuler = realRotation });
            frame.Add(boxEntity, boxPhysicsCollider);
            boxList.Add(boxEntity);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new CreateAndDestroyCollisionbox());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as CreateAndDestroyCollisionbox;
            t.autoDelete = autoDelete;
            t.priority = priority;
            t.collisionboxIdentifier = collisionboxIdentifier;
            t.useExternalShapeConfig = useExternalShapeConfig;
            t.externalShape2DConfigReference = externalShape2DConfigReference;
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
