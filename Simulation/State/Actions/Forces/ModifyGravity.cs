using Photon.Deterministic;
using System;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    [AddTypeMenu(menuName: "Fighter/Movement/Modify Gravity")]
    public unsafe partial class ModifyGravity : HNSFStateAction
    {
        public enum ModifyType
        {
            SET,
            ADD,
            MULTIPLY
        }

        public ModifyType modifyType;
        public HNSFParamFP value;
        public bool asJumpForce;
        public FP multiplier = 1;

        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BattleActorPhysics>(entity, out var actorPhysics)) return false;
            
            switch (modifyType)
            {
                case ModifyType.SET:
                    actorPhysics->SetDynamicVelocityVerticalSpeed(frame, entity, value.Resolve(frame, entity, ref stateContext) * multiplier, asJumpForce);
                    break;
                case ModifyType.ADD:
                    actorPhysics->SetDynamicVelocityVerticalSpeed(frame, entity, actorPhysics->GetDynamicVelocityVerticalSpeedFP(frame, entity) + (value.Resolve(frame, entity, ref stateContext) * multiplier) );
                    break;
                case ModifyType.MULTIPLY:
                    actorPhysics->SetDynamicVelocityVerticalSpeed(frame, entity, actorPhysics->GetDynamicVelocityVerticalSpeedFP(frame, entity) * (value.Resolve(frame, entity, ref stateContext) * multiplier) );
                    break;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new ModifyGravity());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as ModifyGravity;
            t.modifyType = modifyType;
            t.value = value.Clone() as HNSFParamFP;
            t.asJumpForce = asJumpForce;
            t.multiplier = multiplier;
            return base.CopyTo(target);
        }
    }
}