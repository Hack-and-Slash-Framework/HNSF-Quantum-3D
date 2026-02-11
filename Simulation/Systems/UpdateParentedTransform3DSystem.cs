using Photon.Deterministic;
using Quantum;
using UnityEngine.Scripting;

namespace HnSF.core.systems
{
    [Preserve]
    public unsafe class UpdateParentedTransform3DSystem : SystemMainThreadFilter<UpdateParentedTransform3DSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Parented3D* Parented;
            public Transform3D* Transform;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            if (!f.Unsafe.TryGetPointer<Transform3D>(filter.Parented->parent, out var parentTransform)) return;
            
            filter.Transform->Position = parentTransform->Position + parentTransform->TransformDirection(filter.Parented->localOffset);
            filter.Transform->Rotation = parentTransform->Rotation * FPQuaternion.Euler(filter.Parented->localEuler);
        }
    }
}