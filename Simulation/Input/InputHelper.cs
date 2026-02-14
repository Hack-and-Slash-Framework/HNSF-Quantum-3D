using Photon.Deterministic;

namespace Quantum
{
    public static unsafe partial class InputHelper
    {
        public static FPVector3 GetMovementVector(Frame frame, EntityRef entityRef, short offset = 0, bool ignoreY = true)
        {
            if(!frame.Unsafe.TryGetPointer<ActorInputCamera>(entityRef, out var bufferCamera)
               || !frame.Unsafe.TryGetPointer<ActorInputBufferMovement>(entityRef, out var bufferMovement)) return FPVector3.Zero;
            return GetMovementVector(bufferCamera->GetForward(offset), bufferCamera->GetRight(offset),
                bufferMovement->GetMovement(offset), ignoreY);
        }

        public static FPVector3 GetMovementVector(FPVector3 forward, FPVector3 right, FPVector2 moveInput, bool ignoreY = true)
        {
            return GetMovementVector(forward, right, moveInput.X, moveInput.Y, ignoreY);
        }

        public static FPVector3 GetMovementVector(FPVector3 forward, FPVector3 right, FP horizontal, FP vertical, bool ignoreY = true)
        {
            if (ignoreY)
            {
                forward.Y = 0;
                right.Y = 0;
            }

            forward = forward.Normalized;
            right = right.Normalized;

            return forward * vertical + right * horizontal;
        }

        public static FPVector3 GetMovementVector_PreventSpiral(FPVector3 forward, FPVector3 right, FP horizontal,
            FP vertical, FP targetDistance, FP speed, FP rotCorrection)
        {
            forward.Y = 0;
            right.Y = 0;

            // Allows circle strafing instead of spiraling motion when directly right/left.
            if (vertical.RawValue == FP.RAW_ZERO && horizontal.RawValue != FP.RAW_ZERO)
            {
                var realRotCorrection = speed * rotCorrection;
                var fakeDist = FPMath.Clamp((targetDistance - 2), 0, FP.MaxValue);
                realRotCorrection = FPMath.Lerp(-realRotCorrection, realRotCorrection,
                    FPMath.Clamp(fakeDist / FP.FromRaw(327680), 0, FP.FromRaw(FP.RAW_ONE)));
                var horizontalSign = FPMath.Sign(horizontal);
                right = FPQuaternion.Euler(0, -realRotCorrection * horizontalSign, 0) * right;
            }

            forward = forward.Normalized;
            right = right.Normalized;

            return forward * vertical + right * horizontal;
        }
    }
}