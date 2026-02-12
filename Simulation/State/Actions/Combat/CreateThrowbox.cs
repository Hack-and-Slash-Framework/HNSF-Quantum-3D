using Photon.Deterministic;
using System;
using System.Linq;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class CreateThrowbox : HNSFStateAction
    {
        public int throwboxIdentifier;
        public int priority;
        public AssetRef<ThrowInfo> throwInfoReference;

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
            
            var hitboxList = frame.ResolveList(boxCombatant->throwboxList);

            if (boxCombatant->ThrowboxExistWithId(frame, throwboxIdentifier))
            {
                Log.Debug($"THrowbox of id {throwboxIdentifier} already exist on entity {entity.ToString()}. Error came from state {frame.FindAsset<HNSFState>(stateContext.workingState).Label}");
                return false;
            }

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
            
            var hitboxEntity = frame.Create();
            
            var boxPhysicsCollider = new PhysicsCollider3D
            {
                Layer = frame.Layers.GetLayerIndex(HnSFConstants.Layer_Throwbox),
                IsTrigger = true,
                Shape = shape
            };

            frame.Add(hitboxEntity, new Throwbox() { active = true, priority = priority, owner = entity, throwInfoRef = throwInfoReference, id = throwboxIdentifier});
            frame.Add(hitboxEntity, new Transform3D(){ Position = transform->Position + transform->TransformDirection(realOffset), Rotation = FPQuaternion.Euler(transform->EulerAngles + realRotation)});
            frame.Add(hitboxEntity, new Parented3D() { parent = entity, localOffset = realOffset, localEuler = realRotation });
            frame.Add(hitboxEntity, boxPhysicsCollider);
            
            hitboxList.Add(hitboxEntity);
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new CreateThrowbox());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as CreateThrowbox;
            t.throwboxIdentifier = throwboxIdentifier;
            t.priority = priority;
            t.throwInfoReference = throwInfoReference;
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
