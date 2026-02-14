using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.functions
{
    [System.Serializable]
    public unsafe partial class GetBattleActorPhysicsForce : StateFunctionFPVector3
    {
        public enum ForceType
        {
            Movement,
            Gravity,
            Both
        }

        public ForceType forceType;
    
        public override FPVector3 Execute(Frame frame, EntityRef entity, ref HNSFStateContext stateContext)
        {
            var cPhysics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);

            var f = cPhysics->GetOverallVelocity(frame, entity);
            switch (forceType)
            {
                case ForceType.Movement:
                    f.Y = 0;
                    break;
                case ForceType.Gravity:
                    f.X = 0;
                    f.Z = 0;
                    break;
            }

            return f;
        }

        public override HNSFStateFunction Copy()
        {
            return CopyTo(new GetBattleActorPhysicsForce());
        }

        public override HNSFStateFunction CopyTo(HNSFStateFunction target)
        {
            var t = target as GetBattleActorPhysicsForce;
            t.forceType = forceType;
            return base.CopyTo(target);
        }
    }
}