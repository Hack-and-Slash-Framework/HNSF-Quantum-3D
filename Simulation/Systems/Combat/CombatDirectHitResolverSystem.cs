using System.Collections.Generic;
using HnSF.core.state;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.systems
{
    public unsafe partial class CombatDirectHitResolverSystem : CombatHitResolverSystem
    {
        public static List<DirectHitInfo> directHits = new List<DirectHitInfo>();
        
        public override void Update(Frame f)
        {
            for (int i = 0; i < directHits.Count; i++)
            {
                DirectDamage(f, directHits[i]);
            }
            
            directHits.Clear();
        }

        public virtual bool DirectDamage(Frame frame, DirectHitInfo info)
        {
            var result = AttemptHurtActor(
                frame: frame,
                attackerEntityRef: info.attackerEntityRef,
                defenderEntityRef: info.defenderEntityRef,
                attackerHitbox: default,
                attackerHitboxPos: FPVector3.Zero,
                defenderHurtbox: default,
                attackerState: info.hitByState,
                attackerStateId: info.hitByStateIdentifier);

            if (info.checkForStateChange)
            {
                HNSFStateHelper.Generic.CheckForStateChange(frame, info.defenderEntityRef);
            }
            return result;
        }
    }
}