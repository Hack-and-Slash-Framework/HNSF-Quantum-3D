using System.Collections.Generic;
using Quantum;

namespace HnSF.core.systems
{
    public unsafe partial class Combatbox3DBroadphaseReadQueriesSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            ReadWarningboxQueries(f);
            ReadHitboxQueries(f);
            ReadCollisionboxQueries(f);
            ReadThrowboxQueries(f);
            f.Signals.CombatboxResolvingPostNarrowphase();
        }

        protected virtual void ReadWarningboxQueries(Frame f)
        {
            foreach (var warningboxQuery in f.Context.WarningboxBroadphaseQueries)
            {
                if (!f.Physics3D.TryGetQueryHits(warningboxQuery.queryRef, out var hurtboxHits) ||
                    hurtboxHits.Count == 0) continue;

                Warningbox* attackerWarningbox = f.Unsafe.GetPointer<Warningbox>(warningboxQuery.entityRef);
                Hurtbox* defenderHurtbox;

                for (int i = 0; i < hurtboxHits.Count; i++)
                {
                    if (f.Unsafe.TryGetPointer<Hurtbox>(hurtboxHits[i].Entity, out defenderHurtbox))
                    {
                        if (defenderHurtbox->active == false
                            || defenderHurtbox->owner == attackerWarningbox->owner
                            || attackerWarningbox->active == false)
                            continue;

                        var key = new FrameContextUser.ThreatPairKey(warningboxQuery.entityRef, defenderHurtbox->owner);

                        // We only want to bother with one hurtbox for a given defender of this warningbox.
                        if (!f.Context.uniqueThreatPairs.Add(key))
                            continue;
                        
                        var pairEntry = new FrameContextUser.ThreatPairEntry()
                            {
                                attacker = attackerWarningbox->owner,
                                defender = defenderHurtbox->owner,
                                warningbox = attackerWarningbox,
                            };
                        
                        if(!f.Context.defenderToThreatCandidates.ContainsKey(defenderHurtbox->owner))
                            f.Context.defenderToThreatCandidates.Add(defenderHurtbox->owner, new List<FrameContextUser.ThreatPairEntry>(4));
                        f.Context.defenderToThreatCandidates[defenderHurtbox->owner].Add(pairEntry);
                    }
                }
            }
        }
        
        protected virtual void ReadHitboxQueries(Frame f)
        {
            foreach (var hitboxQuery in f.Context.HitboxBroadphaseQueries)
            {
                if (!f.Physics3D.TryGetQueryHits(hitboxQuery.queryRef, out var hitboxHits) ||
                    hitboxHits.Count == 0) continue;

                for (int i = 0; i < hitboxHits.Count; i++)
                {
                    Hitbox* attackerHitbox;
                    Hitbox* defenderHitbox;
                    if (f.Unsafe.TryGetPointer<Hitbox>(hitboxHits[i].Entity, out defenderHitbox))
                    {
                        var pairEntry =
                            new FrameContextUser.PairEntry(hitboxQuery.entityRef, hitboxHits[i].Entity, true);
                        //if (!f.Context.checkedPairs.Add(pairEntry)) continue;

                        attackerHitbox = f.Unsafe.GetPointer<Hitbox>(hitboxQuery.entityRef);
                        defenderHitbox = f.Unsafe.GetPointer<Hitbox>(hitboxHits[i].Entity);
                        if (defenderHitbox->active == false || defenderHitbox->isThrow ||
                            attackerHitbox->owner == defenderHitbox->owner) continue;
                        f.Context.hitboxToHitboxCollisions.Add(pairEntry);
                    }
                    else
                    {
                        Hurtbox* defenderHurtbox;
                        if (f.Unsafe.TryGetPointer<Hurtbox>(hitboxHits[i].Entity, out defenderHurtbox))
                        {
                            var pairEntry =
                                new FrameContextUser.PairEntry(hitboxQuery.entityRef, hitboxHits[i].Entity, true);
                            //if (!f.Context.checkedPairs.Add(pairEntry)) continue;

                            attackerHitbox = f.Unsafe.GetPointer<Hitbox>(hitboxQuery.entityRef);
                            defenderHurtbox = f.Unsafe.GetPointer<Hurtbox>(hitboxHits[i].Entity);
                            if (defenderHurtbox->active == false || attackerHitbox->owner == defenderHurtbox->owner)
                                continue;
                            f.Context.hitboxToHurtboxCollisions.Add(pairEntry);
                        }
                    }
                }
            }
        }

        protected virtual void ReadCollisionboxQueries(Frame f)
        {
            HostProfiler.Start("PARTITION_COLLISIONBOX_to_COLLISIONBOX");
            foreach (var collisionboxQuery in f.Context.CollisionboxBroadphaseQueries)
            {
                if (!f.Physics3D.TryGetQueryHits(collisionboxQuery.queryRef, out var collisionboxHits) ||
                    collisionboxHits.Count == 0) continue;

                Collisionbox* attackerCollisionbox = f.Unsafe.GetPointer<Collisionbox>(collisionboxQuery.entityRef);
                Collisionbox* defenderCollisionbox;

                for (int i = 0; i < collisionboxHits.Count; i++)
                {
                    if (f.Unsafe.TryGetPointer<Collisionbox>(collisionboxHits[i].Entity, out defenderCollisionbox))
                    {
                        if (defenderCollisionbox->active == false ||
                            defenderCollisionbox->owner == attackerCollisionbox->owner) continue;

                        var pairEntry = new FrameContextUser.PairEntry(collisionboxQuery.entityRef,
                            collisionboxHits[i].Entity);
                        if (!f.Context.checkedPairs.Add(pairEntry)) continue;

                        pairEntry.overlapPenetration = collisionboxHits[i].OverlapPenetration;
                        f.Context.collisionboxToCollisionboxCollisions.Add(pairEntry);
                    }
                }
            }
            HostProfiler.End();
        }

        protected virtual void ReadThrowboxQueries(Frame f)
        {
            HostProfiler.Start("PARTITION_THROWBOX_TO_HURTBOX");
            foreach (var throwboxQuery in f.Context.ThrowboxBroadphaseQueries)
            {
                if (!f.Physics3D.TryGetQueryHits(throwboxQuery.queryRef, out var throwboxHits) ||
                    throwboxHits.Count == 0) continue;

                Throwbox* attackerThrowbox = f.Unsafe.GetPointer<Throwbox>(throwboxQuery.entityRef);
                Hurtbox* defenderHurtbox;

                for (int i = 0; i < throwboxHits.Count; i++)
                {
                    if (f.Unsafe.TryGetPointer<Hurtbox>(throwboxHits[i].Entity, out defenderHurtbox))
                    {
                        if (defenderHurtbox->active == false || defenderHurtbox->owner == attackerThrowbox->owner)
                            continue;

                        var pairEntry =
                            new FrameContextUser.PairEntry(throwboxQuery.entityRef, throwboxHits[i].Entity, true);
                        if (!f.Context.checkedPairs.Add(pairEntry)) continue;
                        f.Context.throwboxToHurtboxCollisions.Add(pairEntry);
                    }
                }
            }
            HostProfiler.End();
        }
    }
}