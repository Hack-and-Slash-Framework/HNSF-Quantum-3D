using Photon.Deterministic;

namespace Quantum
{
    public partial struct HitForceData
    {
        public FPVector3 force;
        public bool hasCustomGravity;
        [DrawIf(nameof(hasCustomGravity), true)] public FP hitstunGravity;
        public bool hasCustomTraction;
        [DrawIf(nameof(hasCustomTraction), true)] public FP traction;
        public bool hasCustomAirFriction;
        [DrawIf(nameof(hasCustomAirFriction), true)] public FP friction;
    }
}
