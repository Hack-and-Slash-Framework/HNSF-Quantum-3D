using Quantum;

namespace HnSF.core.systems
{
    public unsafe partial class ThreatZoneResolverSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
            var dangerboxToHurtboxCollisions = frame.Context.defenderToThreatCandidates;

            foreach (var defenderToThreats in dangerboxToHurtboxCollisions)
            {
                EntityRef defender = defenderToThreats.Key;
                
                foreach (var threatPair in defenderToThreats.Value)
                {
                    if (!IsThreatRelevant(frame, threatPair))
                        continue;

                    if (!frame.Context.defenderToPotentialThreats.ContainsKey(defender))
                    {
                        frame.Context.defenderToPotentialThreats.Add(defender, new IncomingThreatsGroupEphemeral());
                    }

                    ResolveThreat(frame, threatPair);
                }
            }
        }

        protected virtual void ResolveThreat(Frame frame, FrameContextUser.ThreatPairEntry pair)
        {
            var context = frame.Context;
            var ephemeralGroup = context.defenderToPotentialThreats[pair.defender];

            var currentEphemeral = CreateIncomingThreat(frame, pair);

            if (IsThreatBetterThan(frame, pair, currentEphemeral, ephemeralGroup.bestOverall))
            {
                ephemeralGroup.bestOverall = currentEphemeral;
            }

            if (pair.warningbox->isThrow)
            {
                if (IsThreatBetterThan(frame, pair, currentEphemeral, ephemeralGroup.bestThrow))
                {
                    ephemeralGroup.bestThrow = currentEphemeral;
                }
            }
            else
            {
                var isProjectile = pair.warningbox->isProjectile;

                if (isProjectile && IsThreatBetterThan(frame, pair, currentEphemeral, ephemeralGroup.bestProjectile))
                {
                    ephemeralGroup.bestProjectile = currentEphemeral;
                }

                if (!isProjectile && IsThreatBetterThan(frame, pair, currentEphemeral, ephemeralGroup.bestStrike))
                {
                    ephemeralGroup.bestStrike = currentEphemeral;
                }
            }

            context.defenderToPotentialThreats[pair.defender] = ephemeralGroup;
        }

        protected virtual IncomingThreatEphemeral CreateIncomingThreat(Frame frame,
            FrameContextUser.ThreatPairEntry pair)
        {
            return new IncomingThreatEphemeral()
            {
                sourceAttacker = pair.attacker,
                isThrow = pair.warningbox->isThrow,
                isProjectile = pair.warningbox->isProjectile,
                impactAtFrame = pair.warningbox->activeAt,
                endAtFrame = pair.warningbox->inactiveAt,
                dangerLevel = pair.warningbox->dangerLevel,
                responseDifficulty = pair.warningbox->responseDifficulty
            };
        }

        protected virtual bool IsThreatBetterThan(Frame frame, FrameContextUser.ThreatPairEntry pair,
            IncomingThreatEphemeral incomingEphemeral, IncomingThreatEphemeral currentEphemeral)
        {
            if (!currentEphemeral.IsValid())
                return true;
            return false;
        }
        
        protected virtual bool IsThreatRelevant(Frame frame, FrameContextUser.ThreatPairEntry pair)
        {
            if (!frame.Unsafe.TryGetPointer<CombatTeam>(pair.attacker, out var attackerTeam)
                || !frame.Unsafe.TryGetPointer<CombatTeam>(pair.defender, out var defenderTeam)
                || !attackerTeam->IsHostileTowards(frame, defenderTeam))
                return false;
            return true;
        }
    }
}