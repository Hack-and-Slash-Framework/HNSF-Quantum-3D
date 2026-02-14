using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Multiply Forces")]
    public unsafe partial class MultiplyForces : HNSFStateAction
    {
        public enum ForceGroupType
        {
            Movement,
            Gravity,
            Both
        }
        
        public ForceGroupType forceToMultiply;
        public HNSFParamFP value;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            BattleActorPhysics* physics = frame.Unsafe.GetPointer<BattleActorPhysics>(entity);

            var multiplyValue = value.Resolve(frame, entity, ref stateContext);

            switch (forceToMultiply)
            {
                case ForceGroupType.Movement:
                    physics->SetKinematicHorizontalSpeed(frame, entity, physics->GetKinematicHorizontalSpeed(frame, entity) * multiplyValue);
                    break;
                case ForceGroupType.Gravity:
                    physics->SetDynamicVelocityVerticalSpeed(frame, entity, physics->GetDynamicVelocityVerticalSpeedFP(frame, entity) * multiplyValue);
                    break;
                case ForceGroupType.Both:
                    var m = physics->GetOverallVelocity(frame, entity);
                    m *= multiplyValue;
                    physics->SetKinematicHorizontalSpeed(frame, entity, m);
                    physics->SetDynamicVelocityVerticalSpeed(frame, entity, m.Y);
                    break;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new MultiplyForces());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as MultiplyForces;
            t.forceToMultiply = forceToMultiply;
            t.value = value.Clone() as HNSFParamFP;
            return base.CopyTo(target);
        }
    }
}