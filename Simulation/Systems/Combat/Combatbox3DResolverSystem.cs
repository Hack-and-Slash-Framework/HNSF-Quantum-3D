using System.Collections.Generic;
using Photon.Deterministic;
using Quantum;
using Quantum.Profiling;

namespace HnSF.core.systems
{
    /// <summary>
    /// The third system for handling combatbox collisions.
    /// This system goes through the collision pairs and finds the highest priority one that should be executed.
    /// Entities can only be hit by one attack source per frame, so a given entity will only ever be in one pair max.
    /// </summary>
    public unsafe partial class Combatbox3DResolverSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            HostProfiler.Start("Combatbox_Resolver");
            
            HostProfiler.Start("CollisionPairs");
            ResolveCollisionPairs(f);
            HostProfiler.End();
            
            HostProfiler.Start("HitboxHitboxPairs");
            ResolveHitboxHitboxPairs(f);
            HostProfiler.End();
            
            HostProfiler.Start("HitboxHurtboxPairs");
            ResolveHitboxHurtboxPairs(f);
            HostProfiler.End();
            
            HostProfiler.Start("ThrowboxHurtboxPairs");
            ResolveThrowboxHurtboxPairs(f);
            HostProfiler.End();
            
            HostProfiler.End();
        }

        private void ResolveThrowboxHurtboxPairs(Frame frame)
        {
            var throwboxpairList = frame.Context.throwboxToHurtboxCollisions;
            if (throwboxpairList.Count == 0) return;
            var throwboxPairs = frame.Context.throwboxPairs;
            
            foreach (var throwCollisionboxPair in throwboxpairList)
            {
                var aThrowbox = frame.Unsafe.GetPointer<Throwbox>(throwCollisionboxPair.boxA);
                var bHurtbox = frame.Unsafe.GetPointer<Hurtbox>(throwCollisionboxPair.boxB);
                if(bHurtbox->owner == aThrowbox->owner) continue;
                
                var combatTeamA = frame.Unsafe.GetPointer<CombatTeam>(aThrowbox->owner);
                var combatTeamB = frame.Unsafe.GetPointer<CombatTeam>(bHurtbox->owner);
                if (!combatTeamA->IsHostileTowards(frame, combatTeamB->value)) continue;
                
                /*
                if(frame.TryFindAsset<HurtboxInfo>(bHurtbox->hurtboxInfoRef, out var hurtboxInfo)
                   && frame.TryFindAsset<ThrowInfo>(aThrowbox->throwInfoRef, out var throwInfo))
                {
                    if(HitAttributeHelper.IsHurtboxAttributeInvincible(frame.SimulationConfig, hurtboxInfo.invincibleAgainstAttributes, throwInfo.attributes)) continue;
                }*/

                var pairKey = new CombatPairKeyAB()
                {
                    entityA = aThrowbox->owner,
                    entityB = bHurtbox->owner
                };
                
                // The thrower has already detected this potential throwee.
                if (throwboxPairs.ContainsKey(pairKey))
                {
                    continue;
                }
                else
                {
                    var newPair = new ThrowboxCombatPair()
                    {
                        entityA = aThrowbox->owner,
                        entityAThrowbox = throwCollisionboxPair.boxA,
                        entityB = bHurtbox->owner,
                        entityBHurtbox = throwCollisionboxPair.boxB
                    };

                    throwboxPairs.Add(pairKey, newPair);
                }
            }
        }

        private void ResolveHitboxHurtboxPairs(Frame frame)
        {
            var hitboxpairList = frame.Context.hitboxToHurtboxCollisions;
            if (hitboxpairList.Count == 0) return;
            var defenderPotentiallyHitBy = frame.Context.defenderPotentiallyHitBy;
            var attackersPotentiallyHitting = frame.Context.attackersPotentiallyHitting;
            
            foreach (var hitboxpair in hitboxpairList)
            {
                var aHitbox = frame.Unsafe.GetPointer<Hitbox>(hitboxpair.boxA);
                var bHurtbox = frame.Unsafe.GetPointer<Hurtbox>(hitboxpair.boxB);
                if (aHitbox->owner == bHurtbox->owner) continue;
                
                var boxCombatantA = frame.Unsafe.GetPointer<BoxCombatant>(aHitbox->owner);
                if (BoxCombatantHelper.HasTouchedEntity(frame, boxCombatantA, bHurtbox->owner)) continue;
                
                var combatTeamA = frame.Unsafe.GetPointer<CombatTeam>(aHitbox->owner);
                var combatTeamB = frame.Unsafe.GetPointer<CombatTeam>(bHurtbox->owner);
                if (!combatTeamA->IsHostileTowards(frame, combatTeamB->value)) continue;
                
                if(frame.TryFindAsset<HurtboxInfo>(bHurtbox->hurtboxInfoRef, out var hurtboxInfo)
                   && frame.TryFindAsset<HitInfo>(aHitbox->hitInfoRef, out var hitInfo))
                {
                    if(HitAttributeHelper.IsHurtboxAttributeInvincible(frame.SimulationConfig, hurtboxInfo.invincibleAgainstAttributes, hitInfo.attributes)) continue;
                }

                if (!frame.Context.defenderPotentiallyHitBy.ContainsKey(bHurtbox->owner))
                {
                    frame.Context.defenderPotentiallyHitBy.Add(bHurtbox->owner, new List<HitboxCombatPair>());
                }
                
                if (frame.Context.TryGetIndexOfAttacker(frame.Context.defenderPotentiallyHitBy[bHurtbox->owner],
                        aHitbox->owner,
                        out var attackerIndex))
                {
                    var hcp = frame.Context.defenderPotentiallyHitBy[bHurtbox->owner][attackerIndex];
                    
                    // Prioritize interactions of higher priorities.
                    if (hcp.attacker == aHitbox->owner && hcp.ignore == false)
                    {
                        if (hcp.defenderHurtboxPriority > bHurtbox->priority)
                            continue;
                        if (hcp.defenderHurtboxPriority == bHurtbox->priority && hcp.attackerHitboxPriority > aHitbox->priority)
                            continue;
                    }
                    
                    hcp.attacker = aHitbox->owner;
                    hcp.attackerHitbox = hitboxpair.boxA;
                    hcp.attackerHitboxPriority = aHitbox->priority;
                    hcp.result = HitboxResolveResult.HitHurtbox;
                    hcp.defenderHurtboxPriority = bHurtbox->priority;
                    hcp.defenderHitboxOrHurtbox = hitboxpair.boxB;
                    hcp.ignore = false;
                    
                    frame.Signals.CombatboxResolvingHitboxHurtboxPairCreatedOrUpdated(&hcp);
                    
                    defenderPotentiallyHitBy[bHurtbox->owner][attackerIndex] = hcp;
                }
                else
                {
                    var newPair = new HitboxCombatPair()
                    {
                        attacker = aHitbox->owner,
                        attackerHitbox = hitboxpair.boxA,
                        attackerHitboxPriority = aHitbox->priority,
                        result = HitboxResolveResult.HitHurtbox,
                        defenderHurtboxPriority = bHurtbox->priority,
                        defenderHitboxOrHurtbox = hitboxpair.boxB,
                        ignore = false
                    };
                    
                    frame.Signals.CombatboxResolvingHitboxHurtboxPairCreatedOrUpdated(&newPair);
                    
                    defenderPotentiallyHitBy[bHurtbox->owner].Add(newPair);
                    
                    if(!attackersPotentiallyHitting.ContainsKey(aHitbox->owner))
                        attackersPotentiallyHitting.Add(aHitbox->owner, new HashSet<EntityRef>());

                    attackersPotentiallyHitting[aHitbox->owner].Add(bHurtbox->owner);
                }
            }
        }

        private void ResolveHitboxHitboxPairs(Frame frame)
        {
            var hitboxpairList = frame.Context.hitboxToHitboxCollisions;
            if (hitboxpairList.Count == 0) return;
            var clashCombatPairs = frame.Context.clashCombatPairs;

            foreach (var hitboxpair in hitboxpairList)
            {
                var hitboxA = frame.Unsafe.GetPointer<Hitbox>(hitboxpair.boxA);
                var hitboxB = frame.Unsafe.GetPointer<Hitbox>(hitboxpair.boxB);
                if (hitboxA->owner == hitboxB->owner) continue;
                
                var boxCombatantA = frame.Unsafe.GetPointer<BoxCombatant>(hitboxA->owner);
                if (BoxCombatantHelper.HasTouchedEntity(frame, boxCombatantA, hitboxB->owner)) continue;
                //var boxCombatantB = frame.Unsafe.GetPointer<BoxCombatant>(hitboxB->owner);
                
                var combatTeamA = frame.Unsafe.GetPointer<CombatTeam>(hitboxA->owner);
                var combatTeamB = frame.Unsafe.GetPointer<CombatTeam>(hitboxB->owner);
                if (!combatTeamA->IsHostileTowards(frame, combatTeamB->value)) continue;

                bool aHasHitInfo = frame.TryFindAsset<HitInfo>(hitboxA->hitInfoRef, out var aHitInfo);
                if (!aHasHitInfo) continue;
                bool bHasHitInfo = frame.TryFindAsset<HitInfo>(hitboxB->hitInfoRef, out var bHitInfo);
                if(!bHasHitInfo) continue;
                
                if (aHitInfo.dontClash || bHitInfo.dontClash) continue;
                if(HitAttributeHelper.CanHitboxesClashBasedOnAttributes(frame.SimulationConfig, aHitInfo.attributes, bHitInfo.attributes) == false) continue;
                
                // If an entity is already clashing with another hitbox.
                if (clashCombatPairs.ContainsKey(hitboxA->owner) || clashCombatPairs.ContainsKey(hitboxB->owner))
                {
                    var key = clashCombatPairs.ContainsKey(hitboxA->owner) ? hitboxA->owner : hitboxB->owner;
                        
                    var cop = clashCombatPairs[key];
                    cop.entityA = hitboxA->owner;
                    cop.entityB = hitboxB->owner;
                    cop.entityAClashLevel = aHitInfo.clashLevel;
                    cop.entityBClashLevel = bHitInfo.clashLevel;
                    cop.entityAHitbox = hitboxpair.boxA;
                    cop.entityBHitbox = hitboxpair.boxB;

                    // If this interaction's clash level difference is larger, it will override the current clash.
                    if (FPMath.Abs(
                            clashCombatPairs[key].entityAClashLevel - clashCombatPairs[key].entityBClashLevel)
                        > FPMath.Abs(cop.entityAClashLevel - cop.entityBClashLevel))
                    {
                        CleanupCurrentClashPair(clashCombatPairs[key].entityA, clashCombatPairs[key].entityB, clashCombatPairs);
                        CleanupCurrentClashPair(hitboxA->owner, hitboxB->owner, clashCombatPairs);
                        clashCombatPairs[key] = cop;
                    }
                }
                else
                {
                    clashCombatPairs.Add(hitboxA->owner, new ClashCombatPair()
                    {
                        entityA = hitboxA->owner,
                        entityB = hitboxB->owner,
                        entityAClashLevel = aHitInfo.clashLevel,
                        entityBClashLevel = bHitInfo.clashLevel,
                        entityAHitbox = hitboxpair.boxA,
                        entityBHitbox = hitboxpair.boxB
                    });
                }
            }
        }
        
        private void CleanupCurrentClashPair(EntityRef keyA, EntityRef keyB, Dictionary<EntityRef, ClashCombatPair> clashCombatPairs)
        {
            clashCombatPairs.Remove(keyA);
            clashCombatPairs.Remove(keyB);
        }

        private void ResolveCollisionPairs(Frame frame)
        {
            var collisionpairList = frame.Context.collisionboxToCollisionboxCollisions;
            var collisionPairs = frame.Context.collisionPairs;
            
            foreach (var collisionPair in collisionpairList)
            {
                var collisionboxA = frame.Unsafe.GetPointer<Collisionbox>(collisionPair.boxA);
                var collisionboxB = frame.Unsafe.GetPointer<Collisionbox>(collisionPair.boxB);
                
                if (collisionPairs.ContainsKey(collisionboxA->owner))
                {
                    if (collisionPairs[collisionboxA->owner].distance > collisionPair.overlapPenetration) continue;

                    var temp = collisionPairs[collisionboxA->owner];
                    temp.entityA = collisionboxA->owner;
                    temp.entityB = collisionboxB->owner;
                    temp.entityACollbox = collisionPair.boxA;
                    temp.entityBCollbox = collisionPair.boxB;
                    temp.distance = collisionPair.overlapPenetration;
                    collisionPairs[collisionboxA->owner] = temp;
                }
                else
                {
                    collisionPairs.Add(collisionboxA->owner, new CollisionCombatPair()
                    {
                        entityA = collisionboxA->owner,
                        entityB = collisionboxB->owner,
                        entityACollbox = collisionPair.boxA,
                        entityBCollbox = collisionPair.boxB,
                        distance = collisionPair.overlapPenetration
                    });
                }
            }
        }
    }
}
