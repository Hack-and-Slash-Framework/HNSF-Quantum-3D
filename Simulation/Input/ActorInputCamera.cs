using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial struct ActorInputCamera
    {
        public FPVector3 GetForward(short offset = 0)
        {
            if(offset >= Constants.CAMERA_BUFFER_SIZE) offset = Constants.CAMERA_BUFFER_SIZE-1;
            return CameraForward[position - offset];
        }
        
        public FPVector3 GetRight(short offset = 0)
        {
            if(offset >= Constants.CAMERA_BUFFER_SIZE) offset = Constants.CAMERA_BUFFER_SIZE-1;
            return CameraRight[position - offset];
        }
        
        public FPVector3 GetMovementVector(short offset, FPVector2 moveInput, bool ignoreY = true)
        {
            return InputHelper.GetMovementVector(GetForward(offset), GetRight(offset), moveInput.X, moveInput.Y, ignoreY);
        }
    }
}