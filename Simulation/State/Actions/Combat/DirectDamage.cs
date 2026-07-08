using Photon.Deterministic;
using System;
using HnSF.core.systems;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class DirectDamage : HNSFStateAction
    {
        public AssetRef<HitInfoBase> hitInfo;
        public bool releaseThrowee;
        public bool immediateStateChange;
        
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            var defenderEntityRef = GetActionTargetEntityRef(frame, entity);
            if (defenderEntityRef == EntityRef.None) return false;
            
            CombatDirectHitResolverSystem.directHits.Add(new DirectHitInfo()
            {
                attackerEntityRef = entity,
                defenderEntityRef = defenderEntityRef,
                hitInfoRef =  hitInfo,
                releaseDefenderFromThrow = releaseThrowee,
                hitboxId = -1,
                hitHurtboxId = -1,
                checkForStateChange = immediateStateChange,
                hitByState = stateContext.workingState,
                hitByStateIdentifier = stateContext.uniqueStateId
            });
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new DirectDamage());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as DirectDamage;
            t.hitInfo = hitInfo;
            t.releaseThrowee = releaseThrowee;
            t.immediateStateChange = immediateStateChange;
            return base.CopyTo(target);
        }
    }
}