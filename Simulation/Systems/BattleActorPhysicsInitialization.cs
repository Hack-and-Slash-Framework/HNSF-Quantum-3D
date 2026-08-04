using Quantum;

namespace HnSF.core.systems
{
    public unsafe class BattleActorPhysicsInitialization : SystemSignalsOnly, ISignalOnComponentAdded<BattleActorPhysics>
    {
        public void OnAdded(Frame f, EntityRef entity, BattleActorPhysics* component)
        {
            component->weight = 100;
            component->pushResistance = 1;
        }
    }
}