using Photon.Deterministic;
using Quantum;

namespace HnSF.core.systems
{
    public unsafe class BattleActorPhysicsBodyMovement : SystemMainThreadFilter<BattleActorPhysicsBodyMovement.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public BattleActorPhysics* charaPhysics;
            public PhysicsBody3D* physicsBody;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            FP multi = 1;
            if (f.Unsafe.TryGetPointer<LocalDeltaTime>(filter.Entity, out var ldt)) multi = ldt->multiplier;
            
            if ((f.Unsafe.TryGetPointer<Hitstop>(filter.Entity, out var hitstop) && hitstop->value > 0) || ldt->deltaTime == 0)
            {
                filter.physicsBody->Velocity = FPVector3.Zero;
                return;
            }
            
            filter.physicsBody->Velocity = filter.charaPhysics->GetOverallVelocity(f, filter.Entity) * multi;
        }
    }
}