using System;
using System.Linq;
using RimWorld;
using Verse;

namespace TarkovInjectors
{
    // Tick-based effect timer allows doses and crashes to survive save/load.
    public class Hediff_InjectorEffect : HediffWithComps
    {
        private int remainingTicks = -1;
        private int pulseTicks;
        private InjectorEffectDef Settings { get { return def as InjectorEffectDef; } }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref remainingTicks, "tci_remainingTicks", -1);
            Scribe_Values.Look(ref pulseTicks, "tci_pulseTicks", 0);
        }

        public void OnInjected()
        {
            if (remainingTicks < 0) remainingTicks = Settings == null ? 2500 : Settings.durationTicks;
            if (Settings != null)
            {
                RemoveToxin(Settings.toxicBuildupReduction);
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (Settings == null || pawn == null || pawn.Dead || delta <= 0) return;
            if (remainingTicks < 0) remainingTicks = Settings.durationTicks;

            // Process only time during which the stimulant is actually active.
            int activeTicks = Math.Min(delta, Math.Max(0, remainingTicks));
            pulseTicks += activeTicks;
            remainingTicks -= delta;
            while (pulseTicks >= 250 && !pawn.Dead)
            {
                pulseTicks -= 250;
                if (Settings.healHPPerHour > 0f) HealInjuries(Settings.healHPPerHour / 10f);
                if (Settings.strainPerHour > 0f) ApplyStrain(Settings.strainPerHour / 10f);
            }
            if (remainingTicks <= 0)
            {
                if (Settings.crashDurationTicks > 0)
                {
                    HediffDef crash = DefDatabase<HediffDef>.GetNamed("TCI_StimulantExhaustion");
                    Hediff active = pawn.health.hediffSet.GetFirstHediffOfDef(crash);
                    if (active != null) pawn.health.RemoveHediff(active);
                    Hediff fresh = pawn.health.AddHediff(crash);
                    if (fresh is Hediff_InjectorEffect crashEffect)
                        crashEffect.OverrideDuration(Settings.crashDurationTicks);
                }
                pawn.health.RemoveHediff(this);
            }
        }

        private void OverrideDuration(int ticks) { remainingTicks = ticks; }

        private void HealInjuries(float points)
        {
            if (points <= 0f) return;
            foreach (Hediff_Injury injury in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList())
            {
                if (points <= 0f) break;
                if (injury.IsPermanent()) continue;
                float healed = Math.Min(points, injury.Severity);
                injury.Heal(healed);
                points -= healed;
            }
        }

        private void RemoveToxin(float amount)
        {
            if (amount <= 0f) return;
            Hediff toxic = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
            if (toxic != null) toxic.Severity = Math.Max(0f, toxic.Severity - amount);
        }

        private void ApplyStrain(float severity)
        {
            HediffDef strain = DefDatabase<HediffDef>.GetNamed("TCI_ChemicalStrain");
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(strain);
            if (existing == null) existing = pawn.health.AddHediff(strain);
            existing.Severity += severity;
        }
    }

    // Treated by a normal doctor using vanilla tending; emergency worsens if ignored.
    public class Hediff_AcuteOverdose : HediffWithComps
    {
        private int pulseTicks;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pulseTicks, "tci_overdosePulse", 0);
        }
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn == null || pawn.Dead || delta <= 0) return;
            pulseTicks += delta;
            while (pulseTicks >= 250 && !pawn.Dead)
            {
                pulseTicks -= 250;
                HediffComp_TendDuration tend = comps == null ? null : comps.OfType<HediffComp_TendDuration>().FirstOrDefault();
                bool stabilized = tend != null && tend.IsTended;
                // At 0.6 initial severity, untreated overdose reaches 1.0 in ~4 game hours.
                // A successfully tended overdose recovers in ~12 game hours.
                Severity += stabilized ? -0.005f : 0.01f;
                if (Severity <= 0f)
                {
                    HediffDef crash = DefDatabase<HediffDef>.GetNamed("TCI_StimulantExhaustion");
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(crash) == null)
                        pawn.health.AddHediff(crash);
                    pawn.health.RemoveHediff(this);
                    break;
                }
            }
        }
    }
}
