using System.Collections.Generic;
using HnSF.core.state;
using HnSF.core.state.actions;
using HnSF.core.state.functions;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.systems
{
    /// <summary>
    /// The forth system for handling combatbox collisions.
    /// Actually executes the hit.
    /// </summary>
    public unsafe class CombatHitResolverSystem : SystemMainThread
    {
        protected List<EntityRef> defendersAlreadyHitThisFrame = new();
    
        public override void Update(Frame f)
        {
            defendersAlreadyHitThisFrame.Clear();
            
            var combatConfig = f.FindAsset(f.RuntimeConfig.combatConfigAssetRef);
            
            var collisionPairs = f.Context.collisionPairs;
            var defenderPotentiallyHit = f.Context.defenderPotentiallyHitBy;
            var clashCombatPairs = f.Context.clashCombatPairs;
            var throwboxPairs = f.Context.throwboxPairs;
            
            foreach (var collisionPair in collisionPairs)
            {
                ResolveCollisionPair(f, collisionPair, combatConfig);
            }
            
            foreach (var clashCombatPair in clashCombatPairs)
            {
                ResolveClashPair(f, defenderPotentiallyHit, clashCombatPair);
            }
            
            foreach (var hitboxCombatPair in defenderPotentiallyHit)
            {
                ResolveHitPair(f, hitboxCombatPair);
            }
            
            foreach (var throwboxCombatPair in throwboxPairs)
            {
                ResolveThrowPair(f, throwboxCombatPair);
            }

            PostResolution(f);
        }

        protected virtual void PostResolution(Frame f)
        {
            // Change state for defenders at the end instead of immediately.
            // Allows us to properly resolve all hits before having boxes be destroyed.
            foreach (var defenderEntityRef in defendersAlreadyHitThisFrame)
            {
                HNSFStateHelper.Generic.CheckForStateChange(f, defenderEntityRef);
            }

            var erhFilter = f.Filter<HNSFEventReceiver>();
            while (erhFilter.NextUnsafe(out var entity, out _))
            {
                EventReceiverHelper.CallEvent(f, entity, (int)EventReceiverTyping.PostCombatboxResolution);
            }
            
            f.Signals.CombatboxResolvingPostResolution();
        }

        protected virtual void ResolveThrowPair(Frame f, KeyValuePair<CombatPairKeyAB, ThrowboxCombatPair> throwboxCombatPair)
        {
            if(defendersAlreadyHitThisFrame.Contains(throwboxCombatPair.Value.entityA)
               || defendersAlreadyHitThisFrame.Contains(throwboxCombatPair.Value.entityB)) return;
                
            var shouldResolve = ThrowEntity(f,
                throwboxCombatPair.Value,
                f.Get<Throwbox>(throwboxCombatPair.Value.entityAThrowbox),
                f.Unsafe.GetPointer<Transform3D>(throwboxCombatPair.Value.entityAThrowbox)->Position);
                
            if (shouldResolve) defendersAlreadyHitThisFrame.Add(throwboxCombatPair.Value.entityB);
        }

        protected virtual void ResolveHitPair(Frame f, KeyValuePair<EntityRef, List<HitboxCombatPair>> hitboxCombatPair)
        {
            if(defendersAlreadyHitThisFrame.Contains(hitboxCombatPair.Key)) return;

            bool shouldMarkEntityAsAlreadyHit = false;
            foreach (var entry in hitboxCombatPair.Value)
            {
                if(entry.ignore) continue;
                    
                shouldMarkEntityAsAlreadyHit = AttemptHurtActor(
                    frame: f,
                    defenderEntityRef: hitboxCombatPair.Key,
                    combatPair: entry,
                    attackerHitbox: f.Get<Hitbox>(entry.attackerHitbox),
                    attackerHitboxPos: f.Unsafe.GetPointer<Transform3D>(entry.attackerHitbox)->Position,
                    attackerState: HNSFStateHelper.GetEntityState(f, entry.attacker),
                    attackerStateId: HNSFStateHelper.GetEntityStateId(f, entry.attacker));

                if (shouldMarkEntityAsAlreadyHit) break;
            }
                
            if (shouldMarkEntityAsAlreadyHit) defendersAlreadyHitThisFrame.Add(hitboxCombatPair.Key);
        }

        protected virtual void ResolveClashPair(Frame f, Dictionary<EntityRef, List<HitboxCombatPair>> defenderPotentiallyHit, KeyValuePair<EntityRef, ClashCombatPair> clashCombatPair)
        {
            //var clashLevelDifference = clashCombatPair.Value.GetClashLevelDifference();
            var isEntityAGettingHit = defenderPotentiallyHit.ContainsKey(clashCombatPair.Value.entityA);
            var isEntityBGettingHit = defenderPotentiallyHit.ContainsKey(clashCombatPair.Value.entityB);
            
            if (isEntityAGettingHit && isEntityBGettingHit)
            {
                // If both entities are potentially getting hit, ignore for now.
                
            }else if (isEntityAGettingHit
                      && f.Context.TryGetIndexOfAttacker(clashCombatPair.Value.entityA, clashCombatPair.Value.entityB, out var attackerIndexB))
            {
                // Check if entityA's hitInfo says trading should take priority.
            }else if (isEntityBGettingHit
                      && f.Context.TryGetIndexOfAttacker(clashCombatPair.Value.entityB, clashCombatPair.Value.entityA, out var attackerIndexA))
            {
                // Check if entityB's hitInfo says trading should take priority.
            }
            else if(isEntityAGettingHit == false && isEntityBGettingHit == false)
            {
                // Neither are getting hit, clash.
                if (!f.Unsafe.TryGetPointer<Hitbox>(clashCombatPair.Value.entityAHitbox, out var hitboxEntityA)
                    || !f.Unsafe.TryGetPointer<Hitbox>(clashCombatPair.Value.entityBHitbox, out var hitboxEntityB)
                    || !f.Unsafe.TryGetPointer<BoxCombatant>(clashCombatPair.Value.entityA, out var boxCombatantA)
                    || !f.Unsafe.TryGetPointer<BoxCombatant>(clashCombatPair.Value.entityB, out var boxCombatantB))
                    return;

                f.AddOrGet<HasClashed>(clashCombatPair.Value.entityA, out var aHasClashed);
                f.AddOrGet<HasClashed>(clashCombatPair.Value.entityA, out var bHasClashed);
                aHasClashed->lastReceivedClashLevel = clashCombatPair.Value.entityAClashLevel;
                bHasClashed->lastReceivedClashLevel = clashCombatPair.Value.entityBClashLevel;
                
                BoxCombatantHelper.MarkEntityAsTouched(f, boxCombatantA, clashCombatPair.Value.entityB, hitboxEntityA->id);
                BoxCombatantHelper.MarkEntityAsTouched(f, boxCombatantB, clashCombatPair.Value.entityA, hitboxEntityB->id);
                MarkEntityAsHavingClashed(f, clashCombatPair.Value.entityA, clashCombatPair.Value.entityB);
                MarkEntityAsHavingClashed(f, clashCombatPair.Value.entityB, clashCombatPair.Value.entityA);
            }
        }

        protected virtual void MarkEntityAsHavingClashed(Frame frame, EntityRef entityRef, EntityRef clashedWith)
        {
            
        }

        protected virtual void ResolveCollisionPair(Frame f, KeyValuePair<EntityRef, CollisionCombatPair> collisionPair, CombatConfiguration combatConfig)
        {
            if (!f.Unsafe.TryGetPointer<Transform3D>(collisionPair.Value.entityACollbox, out var entityACollTransform)
                || !f.Unsafe.TryGetPointer<Transform3D>(collisionPair.Value.entityBCollbox, out var entityBCollTransform))
                return;

            var dir = entityACollTransform->Position - entityBCollTransform->Position;
            dir.Y = 0;
            dir = dir.Normalized;
            dir *= 10;

            var entityAHasActorPhysics = f.Unsafe.TryGetPointer<BattleActorPhysics>(collisionPair.Value.entityA,
                out var entityABattleActorPhysics);
            var entityBHasActorPhysics = f.Unsafe.TryGetPointer<BattleActorPhysics>(collisionPair.Value.entityB,
                out var entityBBattleActorPhysics);
                
            var entityAPushImpulse = dir;
            var entityBPushImpulse = -dir;
                
            if (entityAHasActorPhysics)
            {
                entityBPushImpulse *= entityABattleActorPhysics->pushStrength * (entityBHasActorPhysics ? entityBBattleActorPhysics->selfPushStrength : 1);
            }

            if (entityBHasActorPhysics)
            {
                entityAPushImpulse *= entityBBattleActorPhysics->pushStrength * (entityAHasActorPhysics ? entityABattleActorPhysics->selfPushStrength : 1);
            }
                
            if (entityAHasActorPhysics) entityABattleActorPhysics->SetExternalImpulse(f, collisionPair.Value.entityA, entityAPushImpulse);
            if(entityBHasActorPhysics) entityBBattleActorPhysics->SetExternalImpulse(f, collisionPair.Value.entityB, entityBPushImpulse);
        }
        
        protected virtual bool ThrowEntity(Frame f, ThrowboxCombatPair combatPair,
            Throwbox attackerThrowbox, FPVector3 attackerThrowboxPos)
        {
            if (f.Has<IsBeingThrown>(combatPair.entityB)) return false;
            
            if (!f.Unsafe.TryGetPointer<BoxCombatant>(combatPair.entityB, out var defenderBoxCombatant)
                || !f.Unsafe.TryGetPointer<BoxCombatant>(attackerThrowbox.owner, out var attackerBoxCombatant)
                || !f.Unsafe.TryGetPointer<Hurtbox>(combatPair.entityBHurtbox, out var defenderHurtbox)
                || !f.TryFindAsset<ThrowInfo>(attackerThrowbox.throwInfoRef, out var throwInfo)) return false;

            if (f.TryFindAsset<HurtboxInfo>(defenderHurtbox->hurtboxInfoRef, out var hurtboxInfo) &&
                hurtboxInfo.armor) return false;
            
            if(BoxCombatantHelper.HasTouchedEntity(f, attackerBoxCombatant, combatPair.entityB))
            {
                return false;
            }
            
            // Check Conditions.
            HNSFStateContext defenderContext = new HNSFStateContext(f, combatPair.entityB);
            
            foreach (var cRef in throwInfo.conditions)
            {
                if (cRef.Decide(f, combatPair.entityB, ref defenderContext)) continue;
                return false;
            }
            
            f.AddOrGet<IsThrowing>(combatPair.entityA, out var isThrowing);
            var throweesDict = f.ResolveDictionary(isThrowing->throwees);
            bool didAdd = throweesDict.TryAdd(throwInfo.throweeId, combatPair.entityB);

            if (didAdd == false)
            {
                if (throweesDict.Count == 0) f.Remove<IsThrowing>(combatPair.entityA);
                return false;
            }
            
            var isInThrow = new IsBeingThrown(){ thrower = combatPair.entityA };
            f.Add(combatPair.entityB, isInThrow);

            BoxCombatantHelper.MarkEntityAsTouched(f, attackerBoxCombatant, combatPair.entityB, -1);
            return true;
        }

        protected virtual bool AttemptHurtActor(Frame frame, EntityRef defenderEntityRef, HitboxCombatPair combatPair,
            Hitbox attackerHitbox, FPVector3 attackerHitboxPos, AssetRef<HNSFState> attackerState, uint attackerStateId)
        {
            if (!frame.Unsafe.TryGetPointer<BoxCombatant>(attackerHitbox.owner, out var attackerBoxCombatant))
                return false;

            if (BoxCombatantHelper.HasTouchedEntity(frame, attackerBoxCombatant, defenderEntityRef)) 
                return false;
            
            if (!frame.Unsafe.TryGetPointer<BoxCombatant>(defenderEntityRef, out var defenderBoxCombatant)
                || !frame.Unsafe.TryGetPointer<CombatTeam>(attackerHitbox.owner, out var attackerTeam)
                || !frame.TryFindAsset<HNSFStateFunctionExternal>(defenderBoxCombatant->whenHitReactionFunction.Id, out var hitReactionFunction)
                || !frame.Unsafe.TryGetPointer<Hurtbox>(combatPair.defenderHitboxOrHurtbox, out var defenderHurtbox)) return false;
            
            frame.AddOrGet(defenderEntityRef, out LastHitByInfo* lastHitByInfo);
            
            lastHitByInfo->lastHitOnFrame = frame.Number;
            lastHitByInfo->hitByEntity = combatPair.attacker;
            lastHitByInfo->hitByTeam = attackerTeam->value;
            lastHitByInfo->hitByInfo = new AssetRef<HitInfoBase>(attackerHitbox.hitInfoRef);
            lastHitByInfo->hitByEntityPosition = frame.Unsafe.GetPointer<Transform3D>(combatPair.attacker)->Position;
            lastHitByInfo->hitByPosition = attackerHitboxPos;
            lastHitByInfo->hitByHurtboxWasHit = defenderHurtbox->id;
            lastHitByInfo->hitHurtboxInfo = defenderHurtbox->hurtboxInfoRef;
            lastHitByInfo->hitByState = attackerState;
            lastHitByInfo->hitByStateIdentifier = attackerStateId;
            
            HNSFStateContext defenderStateContext = new HNSFStateContext(frame, defenderEntityRef);
            var hitReactionResultData = (hitReactionFunction.function as StateFunctionHitReactionResultData).Execute(frame, defenderEntityRef, ref defenderStateContext);
            lastHitByInfo->lastReceivedHitReaction = hitReactionResultData;

            frame.Signals.CombatboxResolvingGotHitReactionResult(&combatPair, &hitReactionResultData);

            // ATTACKER
            frame.AddOrGet(combatPair.attacker, out LastHitWithInfo* lastHitWithInfo);
            lastHitWithInfo->data.hitInfoData->lastHitEntity = defenderEntityRef;
            BoxCombatantHelper.MarkEntityAsTouched(frame, attackerBoxCombatant, defenderEntityRef, attackerHitbox.id);
            lastHitWithInfo->data.hitInfoData->lastReceivedHitReaction = hitReactionResultData;
            lastHitWithInfo->data.hitInfoData->hitWithInfo = new AssetRef<HitInfoBase>(attackerHitbox.hitInfoRef);

            if (frame.TryFindAsset<HNSFStateActionExternal>(attackerBoxCombatant->whenGotHitReactionAction.Id,
                    out var whenGotHitReactionAction))
            {
                defenderStateContext = new HNSFStateContext(frame, combatPair.attacker);
                whenGotHitReactionAction.action.ExecuteAction(frame, combatPair.attacker, 0, ref defenderStateContext);
            }
            
            switch (hitReactionResultData.hitReaction)
            {
                case StandardHitReactions.Hit:
                    return true;
                case StandardHitReactions.Blocked:
                    return true;
                case StandardHitReactions.Perfect_Guard:
                    return true;
                case StandardHitReactions.Parried:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DirectDamage(Frame frame, EntityRef attacker, EntityRef defender, AssetRef<HitInfoBase> hitInfoRef,
            int hitboxId = -1, int hitHurtboxId = -1, bool checkForStateChange = true, AssetRef<HNSFState> hitByState = default,
            uint hitByStateIdentifier = 0)
        {
            if (!frame.Unsafe.TryGetPointer<BoxCombatant>(defender, out var defenderBoxCombatant)
                || !frame.Unsafe.TryGetPointer<BoxCombatant>(attacker, out var attackerBoxCombatant)
                || !frame.Unsafe.TryGetPointer<CombatTeam>(attacker, out var attackerTeam)
                || !frame.TryFindAsset<HNSFStateFunctionExternal>(defenderBoxCombatant->whenHitReactionFunction.Id, out var hitReactionFunction)) return false;
            
            frame.AddOrGet(defender, out LastHitByInfo* lastHitByInfo);
            
            lastHitByInfo->lastHitOnFrame = frame.Number;
            lastHitByInfo->hitByEntity = attacker;
            lastHitByInfo->hitByTeam = attackerTeam->value;
            lastHitByInfo->hitByInfo = hitInfoRef;
            lastHitByInfo->hitByEntityPosition = frame.Unsafe.GetPointer<Transform3D>(attacker)->Position;
            lastHitByInfo->hitByPosition = lastHitByInfo->hitByEntityPosition;
            lastHitByInfo->hitByHurtboxWasHit = hitHurtboxId;
            lastHitByInfo->hitHurtboxInfo = default;
            lastHitByInfo->hitByState = hitByState;
            lastHitByInfo->hitByStateIdentifier = hitByStateIdentifier;
            
            HNSFStateContext defenderStateContext = new HNSFStateContext(frame, defender);
            var hitReaction = (hitReactionFunction.function as StateFunctionHitReactionResultData).Execute(frame, defender, ref defenderStateContext);
            lastHitByInfo->lastReceivedHitReaction = hitReaction;
            
            // ATTACKER
            frame.AddOrGet(attacker, out LastHitWithInfo* lastHitWithInfo);
            lastHitWithInfo->data.hitInfoData->lastHitEntity = defender;
            BoxCombatantHelper.MarkEntityAsTouched(frame, attackerBoxCombatant, defender, hitboxId);
            lastHitWithInfo->data.hitInfoData->lastReceivedHitReaction = hitReaction;
            lastHitWithInfo->data.hitInfoData->hitWithInfo = hitInfoRef;

            if (frame.TryFindAsset<HNSFStateActionExternal>(attackerBoxCombatant->whenGotHitReactionAction.Id,
                    out var whenGotHitReactionAction))
            {

                defenderStateContext = new HNSFStateContext(frame, attacker);
                whenGotHitReactionAction.action.ExecuteAction(frame, attacker, 0, ref defenderStateContext);
            }

            return true;
        }
    }
}