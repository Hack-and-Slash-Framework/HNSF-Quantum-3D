using Photon.Deterministic;

namespace Quantum
{
    public static unsafe partial class VisualEffectHelper
    {
        public static bool PlayVisualEffect(Frame frame, PlayVisualEffectRequest request, EntityRef entity,
            FPVector3 position, FPVector3 rotation, FPVector3 closestBodyPosition, bool atClosestBodyPosition = false)
        {
            var vfx = request.GetRngVFX(frame.RNG);
            if (!vfx.vfxReference.IsValid) return false;
            PlayVisualEffect(frame, request, vfx, entity, position, rotation, closestBodyPosition, atClosestBodyPosition);
            return true;
        }

        public static void PlayVisualEffect(Frame frame, PlayVisualEffectRequest request,
            PlayVisualEffectRequest.VFXReference vfx, EntityRef entity,
            FPVector3 position, FPVector3 rotation,
            FPVector3 closestBodyPosition, bool atClosestBodyPosition = false)
        {
            frame.Events.PlayVisualEffectAtLocation3D(
                visualEffectRef: vfx.vfxReference,
                parented: request.parentedToSelf,
                parent: entity,
                positionAsOffset: request.positionAsOffset,
                position: request.positionAsOffset ? request.positionOffset : position,
                rotationAsOffset: request.rotationAsOffset,
                rotation: request.rotationAsOffset ? request.rotationOffset : rotation,
                atClosestBodyPosition: atClosestBodyPosition,
                sourcePosition: closestBodyPosition,
                setRotationToForceDir: request.rotateToMoveForce,
                parentBoneTag: request.parentBoneTag
            );
        }

        public static bool PlayVisualEffect(Frame frame, PlayVisualEffectRequest request, EntityRef entity,
            bool positionAsOffset, FPVector3 position, bool rotationAsOffset, FPVector3 rotation,
            FPVector3 closestBodyPosition, bool atClosestBodyPosition = false)
        {
            var vfx = request.GetRngVFX(frame.RNG);
            if (!vfx.vfxReference.IsValid) return false;
            PlayVisualEffect(frame, request, vfx, entity, positionAsOffset, position, rotationAsOffset, rotation,
                closestBodyPosition, atClosestBodyPosition);
            return true;
        }

        public static void PlayVisualEffect(Frame frame, PlayVisualEffectRequest request,
            PlayVisualEffectRequest.VFXReference vfx, EntityRef entity,
            bool positionAsOffset, FPVector3 position, bool rotationAsOffset, FPVector3 rotation,
            FPVector3 closestBodyPosition, bool atClosestBodyPosition = false)
        {
            frame.Events.PlayVisualEffectAtLocation3D(
                visualEffectRef: vfx.vfxReference,
                parented: request.parentedToSelf,
                parent: entity,
                positionAsOffset: positionAsOffset,
                position: position,
                rotationAsOffset: rotationAsOffset,
                rotation: rotation,
                atClosestBodyPosition: atClosestBodyPosition,
                sourcePosition: closestBodyPosition,
                setRotationToForceDir: request.rotateToMoveForce,
                parentBoneTag: request.parentBoneTag
            );
        }
    }
}
