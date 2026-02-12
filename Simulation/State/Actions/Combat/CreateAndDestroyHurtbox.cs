using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class CreateAndDestroyHurtbox : HNSFStateAction
    {
        public bool autoDelete = true;
        public int hurtboxIdentifier;
        public int priority;
        public AssetRef<HurtboxInfo> hurtboxInfoReference;
        
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
                boxCombatant->DeleteHurtboxByID(frame, hurtboxIdentifier);
                return false;
            }
            if (boxCombatant->HurtboxExistWithId(frame, hurtboxIdentifier)) return false;
            
            
            var hurtboxList = frame.ResolveList(boxCombatant->hurtboxList);
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
                Layer = frame.Layers.GetLayerIndex(HnSFConstants.Layer_Hurtbox),
                IsTrigger = true,
                Shape = shape
            };
                
            var boxEntity = frame.Create();
            frame.Add(boxEntity, new Hurtbox() { active = true, priority = priority, owner = entity, hurtboxInfoRef = hurtboxInfoReference, id = hurtboxIdentifier});
            frame.Add(boxEntity, new Transform3D(){ Position = transform->Position + transform->TransformDirection(realOffset), Rotation = FPQuaternion.Euler(transform->EulerAngles + realRotation)});
            frame.Add(boxEntity, new Parented3D() { parent = entity, localOffset = realOffset, localEuler = realRotation });
            frame.Add(boxEntity, boxPhysicsCollider);
            hurtboxList.Add(boxEntity);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new CreateAndDestroyHurtbox());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as CreateAndDestroyHurtbox;
            t.autoDelete = autoDelete;
            t.hurtboxIdentifier = hurtboxIdentifier;
            t.priority = priority;
            t.hurtboxInfoReference = hurtboxInfoReference;
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
