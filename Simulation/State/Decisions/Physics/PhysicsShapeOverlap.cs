using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.decisions
{
    [Serializable]
    public unsafe partial class PhysicsShapeOverlap : HNSFStateDecision
    {
        public LayerMaskParam layerMask;
        public Shape3DType shape;
        [DrawIf(nameof(shape), (int)Shape3DType.Sphere)] public FP radius;
        [DrawIf(nameof(shape), (int)Shape3DType.Box)] public FPVector3 boxExtents;
        public FPVector3 offset;
        public bool debugDraw;
        
        public override bool Decide(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entity);

            Shape3D s = shape switch
            {
                Shape3DType.Sphere => Shape3D.CreateSphere(radius, offset),
                Shape3DType.Box => Shape3D.CreateBox(boxExtents, offset),
                _ => default
            };

            var hits = frame.Physics3D.OverlapShape(transform->Position, transform->Rotation, s, layerMask.Get(frame), QueryOptions.HitAll);
            
            if (debugDraw)
            {
                Draw.Shape(frame, ref s, transform->Position, transform->Rotation, ColorRGBA.Green);
            }
            
            for (int i = 0; i < hits.Count; i++)
            {
                var h = hits[i];

                if (EntityIsSelfOrOwnedBySelf(frame, entity, h.Entity)) continue;
                return true;
            }
            return false;
        }

        private bool EntityIsSelfOrOwnedBySelf(Frame frame, EntityRef entity, EntityRef hEntity)
        {
            if (entity == hEntity) return true;

            // TODO: Support more than 1 layer deep parents.
            if (frame.Unsafe.TryGetPointer<Parented3D>(hEntity, out var par))
            {
                if (par->parent == entity) return true;
            }
            
            return false;
        }

        public override HNSFStateDecision Copy()
        {
            return CopyTo(new PhysicsShapeOverlap());
        }

        public override HNSFStateDecision CopyTo(HNSFStateDecision target)
        {
            var t = target as PhysicsShapeOverlap;
            t.layerMask = new LayerMaskParam()
            {
                source = layerMask.source,
                externalLayerMask = layerMask.externalLayerMask,
                layerMask = layerMask.layerMask,
            };
            t.shape = shape;
            t.radius = radius;
            t.boxExtents = boxExtents;
            t.offset = offset;
            return base.CopyTo(target);
        }
    }
}