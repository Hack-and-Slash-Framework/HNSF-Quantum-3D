using Photon.Deterministic;

namespace Quantum
{
    public unsafe class TransformHelpers
    {
        public static FPVector3 GetCenterPosition(Frame frame, EntityRef entityRef, Transform3D* transform)
        {
            if (frame.Unsafe.TryGetPointer<KCC>(entityRef, out var kcc))
            {
                return GetCenterPosition(transform, kcc);
            }
            return transform->Position;
        }
        
        public static FPVector3 GetCenterOfMass(Frame frame, EntityRef entityRef)
        {
            var transform = frame.Unsafe.GetPointer<Transform3D>(entityRef);
            if (frame.Unsafe.TryGetPointer<KCC>(entityRef, out var kcc))
            {
                return GetCenterPosition(transform, kcc);
            }
            return transform->Position;
        }
        
        public static FPVector3 GetCenterPosition(Transform3D* transform, KCC* kcc)
        { 
            return transform->Position + transform->TransformDirection(kcc->Data.PositionOffset) + new FPVector3(0, kcc->Data.Height / FP._2, 0);
        }
    }
}