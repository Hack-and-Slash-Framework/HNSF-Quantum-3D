using System;
using System.Collections.Generic;
using HnSF;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial class FrameContextUser
    {
        public struct ThreatPairKey : IEquatable<ThreatPairKey>
        {
            public readonly EntityRef Warningbox;
            public readonly EntityRef Defender;

            public ThreatPairKey(EntityRef warningbox, EntityRef defender)
            {
                Warningbox = warningbox;
                Defender = defender;
            }

            public bool Equals(ThreatPairKey other)
            {
                return Warningbox == other.Warningbox &&
                       Defender == other.Defender;
            }

            public override bool Equals(object obj)
            {
                return obj is ThreatPairKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Warningbox.GetHashCode() * 397) ^
                           Defender.GetHashCode();
                }
            }
        }
        
        public struct ThreatPairEntry
        {
            public EntityRef attacker;
            public EntityRef defender;
            public Warningbox* warningbox;
        }
        
        public struct PairEntry : IEquatable<PairEntry>
        {
            public EntityRef boxA;
            public EntityRef boxB;
            public FP overlapPenetration;

            public PairEntry(EntityRef a, EntityRef b)
            {
                this.boxA = a.Index < b.Index ? a : b;
                this.boxB = this.boxA == a ? b : a;
                overlapPenetration = 0;
            }
            
            public PairEntry(EntityRef a, EntityRef b, bool dontSort)
            {
                this.boxA = a;
                this.boxB = b;
                //this.boxA = a.Index < b.Index ? a : b;
                //this.boxB = this.boxA == a ? b : a;
                overlapPenetration = 0;
            }
            
            public PairEntry(EntityRef a, EntityRef b, FP overlap)
            {
                this.boxA = a.Index < b.Index ? a : b;
                this.boxB = this.boxA == a ? b : a;
                overlapPenetration = overlap;
            }
            
            public bool Equals(PairEntry other)
            {
                return boxA.Equals(other.boxA) && boxB.Equals(other.boxB);
            }

            public override bool Equals(object obj)
            {
                return obj is PairEntry other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(boxA, boxB);
            }
        }
        
        public readonly int gridSize = 5;
        
        public HashSet<PairEntry> checkedPairs = new();

        public HashSet<PairEntry> hitboxToHitboxCollisions = new();
        public HashSet<PairEntry> hitboxToHurtboxCollisions = new();
        public HashSet<PairEntry> collisionboxToCollisionboxCollisions = new();
        public HashSet<PairEntry> throwboxToHurtboxCollisions = new();
        
        public HashSet<ThreatPairKey> uniqueThreatPairs = new();
        public Dictionary<EntityRef, List<ThreatPairEntry>> defenderToThreatCandidates = new();
        
        public Dictionary<EntityRef, List<HitboxCombatPair>> defenderPotentiallyHitBy = new();
        public Dictionary<EntityRef, HashSet<EntityRef>> attackersPotentiallyHitting = new();
        public Dictionary<EntityRef, ClashCombatPair> clashCombatPairs = new();
        public Dictionary<EntityRef, CollisionCombatPair> collisionPairs = new();
        public Dictionary<CombatPairKeyAB, ThrowboxCombatPair> throwboxPairs = new();

        public Dictionary<EntityRef, IncomingThreatsGroupEphemeral> defenderToPotentialThreats = new();
        
        // QUEURIES
        public struct EntityToPhysicsQuery
        {
            public EntityRef entityRef;
            public PhysicsQueryRef queryRef;
        }
        public List<EntityToPhysicsQuery> HitboxBroadphaseQueries = new List<EntityToPhysicsQuery>(20);
        public List<EntityToPhysicsQuery> HurtboxBroadphaseQueries = new List<EntityToPhysicsQuery>(20);
        public List<EntityToPhysicsQuery> CollisionboxBroadphaseQueries = new List<EntityToPhysicsQuery>(20);
        public List<EntityToPhysicsQuery> ThrowboxBroadphaseQueries = new List<EntityToPhysicsQuery>(20);
        public List<EntityToPhysicsQuery> WarningboxBroadphaseQueries = new List<EntityToPhysicsQuery>(20);

        
        // CULLING
        public delegate bool CullingDelegate(FPVector3 position);
        public CullingDelegate CullingCallback;
        
        public void ClearFrameCombatResolutionVariables()
        {
            checkedPairs.Clear();
            
            hitboxToHitboxCollisions.Clear();
            hitboxToHurtboxCollisions.Clear();
            collisionboxToCollisionboxCollisions.Clear();
            throwboxToHurtboxCollisions.Clear();
            
            uniqueThreatPairs.Clear();
            defenderToThreatCandidates.Clear();
            
            defenderPotentiallyHitBy.Clear();
            attackersPotentiallyHitting.Clear();
            clashCombatPairs.Clear();
            collisionPairs.Clear();
            throwboxPairs.Clear();
            
            HitboxBroadphaseQueries.Clear();
            HurtboxBroadphaseQueries.Clear();
            CollisionboxBroadphaseQueries.Clear();
            ThrowboxBroadphaseQueries.Clear();
            WarningboxBroadphaseQueries.Clear();
            
            defenderToPotentialThreats.Clear();
        }
        
        public int GetIndexOfAttacker(EntityRef defenderEntityRef, EntityRef attackerRef)
        {
            if (!defenderPotentiallyHitBy.ContainsKey(defenderEntityRef))
            {
                return -1;
            }

            return GetIndexOfAttacker(defenderPotentiallyHitBy[defenderEntityRef], attackerRef);
        }
        
        public int GetIndexOfAttacker(List<HitboxCombatPair> pairList, EntityRef attackerRef)
        {
            for (int i = 0; i < pairList.Count; i++)
            {
                if(pairList[i].attacker != attackerRef) continue;
                return i;
            }
            return -1;
        }
        
        public bool TryGetIndexOfAttacker(EntityRef defenderEntityRef, EntityRef attackerRef, out int index)
        {
            if (!defenderPotentiallyHitBy.ContainsKey(defenderEntityRef))
            {
                index = -1;
                return false;
            }
            return TryGetIndexOfAttacker(defenderPotentiallyHitBy[defenderEntityRef], attackerRef, out index);
        }
        
        public bool TryGetIndexOfAttacker(List<HitboxCombatPair> pairList, EntityRef attackerRef, out int index)
        {
            index = -1;
            for (int i = 0; i < pairList.Count; i++)
            {
                if(pairList[i].attacker != attackerRef) continue;
                index = i;
                return true;
            }
            return false;
        }
    }
}
