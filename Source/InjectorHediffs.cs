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
                if (Settings.clotBleeding) ClotWounds();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Settings == null || pawn == null || pawn.Dead) return;
            if (remainingTicks < 0) remainingTicks = Settings.durationTicks;
            remainingTicks--;
            if (++pulseTicks >= 250)
            {
                pulseTicks = 0;
                if (Settings.healHPPerHour > 0f) HealInjuries(Settings.healHPPerHour / 10f);
                if (Settings.clotBleeding) ClotWounds();
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
                    if (fresh is Hediff_InjectorEffect crashEffect) crashEffect.OverrideDuration(Settings.crashDurationTicks);
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

        private void ClotWounds()
        {
            // This intentionally uses the vanilla wound-tending path: prevents bleeding
            // without replacing lost blood. New bleeding wounds are caught next pulse.
            foreach (Hediff_Injury injury in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList())
                if (injury.Bleeding && injury.TendableNow()) injury.Tended(1f, 1f);
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
        public override void Tick()
        {
            base.Tick();
            if (pawn == null || pawn.Dead || ++pulseTicks < 250) return;
            pulseTicks = 0;
            HediffComp_TendDuration tend = TryGetComp<HediffComp_TendDuration>();
            bool stabilized = tend != null && tend.IsTended;
            // Untreated: lethal within ~4 game hours. Tended: ~12h recovery.
            Severity += stabilized ? -0.005f : 0.025f;
            if (Severity <= 0f)
            {
                HediffDef crash = DefDatabase<HediffDef>.GetNamed("TCI_StimulantExhaustion");
                Hediff old = pawn.health.hediffSet.GetFirstHediffOfDef(crash);
                if (old == null)
                {
                    Hediff fresh = pawn.health.AddHediff(crash);
                }
                pawn.health.RemoveHediff(this);
            }
        }
    }
}
