using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Movement/Prevent Fall Off Edge")]
    public unsafe partial class PreventFallOffEdge : HNSFStateAction
    {
        public FP multi = 1;
        public ExternalLayerMask layerMask;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            /*
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var bap)
                || !frame.Unsafe.TryGetPointer<Transform3D>(entity, out var entityTransform)) return false;

            var force = bap->GetKinematicHorizontalSpeed(frame, entity);
            
            if (force.SqrMagnitude <= FP.SmallestNonZero) return false;
            var centerPos = bap->GetCenter(frame, entity);
            var forceMag = force.Magnitude;
            
            var h1 = frame.Physics3D.Raycast(
                centerPos,
                entityTransform->Forward,
                forceMag,
                layerMask.mask
            );
            if (h1.HasValue) return false;

            var h2 = frame.Physics3D.Raycast(
                centerPos + (entityTransform->Forward * forceMag * multi),
                FPVector3.Down,
                );*/
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new PreventFallOffEdge());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as PreventFallOffEdge;
            t.multi = multi;
            t.layerMask = layerMask;
            return base.CopyTo(target);
        }
    }
}