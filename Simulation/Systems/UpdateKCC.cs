using Quantum;

namespace HnSF.core.systems
{
    public unsafe class UpdateKCC : SystemMainThreadFilter<UpdateKCC.Filter>, ISignalOnComponentAdded<KCC>, ISignalOnComponentRemoved<KCC>
    {
        public override void Update(Frame frame, ref Filter filter)
        {
            if (frame.Unsafe.TryGetPointer<Hitstop>(filter.Entity, out var hitstop) && hitstop->value > 0) return;
            KCCContext context = KCCContext.Get(frame, filter.Entity, filter.KCC);
            filter.KCC->Update(context);
            KCCContext.Return(context);
        }

        void ISignalOnComponentAdded<KCC>.OnAdded(Frame frame, EntityRef entity, KCC* kcc)
        {
            KCCContext context = KCCContext.Get(frame, entity, kcc);
            kcc->Initialize(context);
            KCCContext.Return(context);
        }

        void ISignalOnComponentRemoved<KCC>.OnRemoved(Frame frame, EntityRef entity, KCC* kcc)
        {
            KCCContext context = KCCContext.Get(frame, entity, kcc);
            kcc->Deinitialize(context);
            KCCContext.Return(context);
        }

        public struct Filter
        {
            public EntityRef Entity;
            public KCC*      KCC;
        }
    }
}