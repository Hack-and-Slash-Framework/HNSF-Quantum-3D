using System;
using Photon.Deterministic;
using Quantum;

namespace HnSF.core.state.actions
{
    [Serializable]
    public unsafe partial class HitResponseStandard : HNSFStateAction
    {
        public override bool ExecuteAction(Frame frame, EntityRef entity, FP rangePercent,
            ref HNSFStateContext stateContext)
        {
            if (!frame.Unsafe.TryGetPointer<BoxCombatant>(entity, out var boxCombatant)) return false;
            if (!frame.Unsafe.TryGetPointer<LastHitWithInfo>(entity, out var lastHitWithInfo)) return false;
            if (lastHitWithInfo->data.Field != Quantum.LastHitWithData.HITINFODATA
                || !frame.TryFindAsset<HitInfo>(lastHitWithInfo->data.hitInfoData->hitWithInfo.Id, out var hitWithInfo)) return false;

            var hbcDictionary = frame.ResolveDictionary(boxCombatant->hitReactionCounters);

            var hasHitstop = frame.Unsafe.TryGetPointer<Hitstop>(entity, out var hitstop);
            
            var hitReactionInfo = lastHitWithInfo->data.hitInfoData->lastReceivedHitReaction;

            var attackerHitstop = hitWithInfo.attackerHitstop;
            
            switch (hitReactionInfo.hitReaction)
            {
                case StandardHitReactions.Missed:
                    break;
                case StandardHitReactions.Hit:
                    hbcDictionary.TryAdd((int)StandardHitReactions.Hit, 0);
                    hbcDictionary[(int)StandardHitReactions.Hit]++;
                    if(hasHitstop) hitstop->value = attackerHitstop;
                    lastHitWithInfo->lastHitEntityOnFrame = frame.Number;
                    lastHitWithInfo->data.hitInfoData->lastHitstopAmount = hitWithInfo.attackerHitstop;

                    if (hitWithInfo.assignThroweeOnHit
                        && frame.Exists(lastHitWithInfo->data.hitInfoData->lastHitEntity))
                    {
                        CombatHelper.ThrowHelper.GrabEntity(frame, entity, lastHitWithInfo->data.hitInfoData->lastHitEntity);
                    }
                    break;
                case StandardHitReactions.Blocked:
                    hbcDictionary.TryAdd((int)StandardHitReactions.Blocked, 0);
                    hbcDictionary[(int)StandardHitReactions.Blocked]++;
                    if(hasHitstop) hitstop->value = attackerHitstop;
                    lastHitWithInfo->lastHitEntityOnFrame = frame.Number;
                    lastHitWithInfo->data.hitInfoData->lastHitstopAmount = hitWithInfo.attackerHitstop;
                    break;
                case StandardHitReactions.Parried:
                    break;
            }
            return false;
        }

        public override HNSFStateAction Copy()
        {
            return CopyTo(new HitResponseStandard());
        }

        public override HNSFStateAction CopyTo(HNSFStateAction target)
        {
            var t = target as HitResponseStandard;
            return base.CopyTo(target);
        }
    }
}