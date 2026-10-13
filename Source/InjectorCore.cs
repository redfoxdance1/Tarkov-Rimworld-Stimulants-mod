using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TarkovInjectors
{
    // None of the injector ThingDefs are ingestible or CompProperties_Drug.
    // This means no joy-use, scheduled drug use, addiction-driven use, or mental-break ingestion.
    public class InjectorEffectDef : HediffDef
    {
        public int durationTicks = 2500;
        public float healHPPerHour = 0f;
        public bool clotBleeding = false;
        public float toxicBuildupReduction = 0f;
        public float strainPerHour = 0f;
        public int crashDurationTicks = 0;
    }

    public class CompProperties_Injector : CompProperties
    {
        public HediffDef effect;
        public bool originalObdolbos = false;
        public bool randomObdolbos = false;
        public CompProperties_Injector() { compClass = typeof(CompInjector); }
    }

    public class CompInjector : ThingComp
    {
        public CompProperties_Injector InjectorProps { get { return (CompProperties_Injector)props; } }
    }

    public class CompProperties_InjectorController : CompProperties
    {
        public CompProperties_InjectorController() { compClass = typeof(CompInjectorController); }
    }

    public class Command_Inject : Command_Action
    {
        private readonly CompInjectorController controller;
        public Command_Inject(CompInjectorController owner)
        {
            controller = owner;
            defaultLabel = "Inject";
            defaultDesc = "Choose an injector carried by this pawn. Only a direct player order can administer it. Right-click to open the list.";
            icon = ContentFinder<Texture2D>.Get("TarkovInjectors/InjectButton", true);
            action = () => controller.OpenMenu(); // Left-click works too.
        }
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get { return controller.MakeOptions(); }
        }
    }

    public class CompInjectorController : ThingComp
    {
        private int stimLoad;
        private int nextDecayTick;
        private const int DecayInterval = 15000; // 6 in-game hours
        private static readonly float[] Risks = {0f, .2f, .4f, .8f, .995f};
        public int StimLoad { get { return stimLoad; } }
        public float NextRisk { get { return Risks[Math.Min(stimLoad, 4)]; } }
        private Pawn Pawn { get { return parent as Pawn; } }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stimLoad, "tci_stimLoad", 0);
            Scribe_Values.Look(ref nextDecayTick, "tci_nextDecayTick", 0);
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (stimLoad <= 0 || Find.TickManager == null) return;
            int tick = Find.TickManager.TicksGame;
            // A skipped/large tick interval must not postpone recovery.
            while (stimLoad > 0 && nextDecayTick > 0 && tick >= nextDecayTick)
            {
                stimLoad--;
                nextDecayTick = stimLoad > 0 ? nextDecayTick + DecayInterval : 0;
            }
        }

        public override string CompInspectStringExtra()
        {
            return stimLoad > 0 ? "Stimulant load: " + stimLoad + " | Next overdose chance: " + (NextRisk * 100f).ToString("0.#") + "%" : null;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn != null && Pawn.IsColonistPlayerControlled && Pawn.inventory != null)
            {
                Command_Inject button = new Command_Inject(this);
                if (!AvailableInjectors().Any() || Pawn.Downed || Pawn.Dead || Pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) <= 0f)
                    button.Disable("No available syringes, or pawn cannot self-inject.");
                yield return button;
            }
        }

        // Right-clicking another incapacitated colonist provides player-ordered doctor administration.
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            Pawn patient = Pawn;
            if (patient == null || !patient.Downed || patient.Dead || selPawn == null ||
                selPawn == patient || !selPawn.IsColonistPlayerControlled || selPawn.Downed ||
                selPawn.inventory == null || selPawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) <= 0f)
                yield break;
            foreach (Thing item in selPawn.inventory.innerContainer)
            {
                if (item.TryGetComp<CompInjector>() == null) continue;
                Thing syringe = item;
                yield return new FloatMenuOption("Administer " + item.LabelCap + " to " + patient.LabelShortCap,
                    () => RequestDoctorInjection(selPawn, syringe));
            }
        }

        private void RequestDoctorInjection(Pawn doctor, Thing syringe)
        {
            CompInjector injection = syringe.TryGetComp<CompInjector>();
            if (injection == null) return;
            bool fatal = IsObdolbosActive() || injection.InjectorProps.originalObdolbos;
            Action start = () =>
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TCI_InjectPatient"), Pawn, syringe);
                doctor.jobs.TryTakeOrderedJob(job);
            };
            if (NextRisk <= 0f) { start(); return; }
            string warning = Pawn.LabelShortCap + " has a " + (NextRisk * 100f).ToString("0.#") +
                "% overdose risk. " + (fatal ? "Obdolbos makes an overdose instantly fatal." :
                "An overdose causes a medical emergency requiring tending.") + "\nProceed?";
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(warning, start));
        }

        private IEnumerable<Thing> AvailableInjectors()
        {
            if (Pawn == null || Pawn.inventory == null) yield break;
            foreach (Thing item in Pawn.inventory.innerContainer)
                if (item.TryGetComp<CompInjector>() != null && item.stackCount > 0)
                    yield return item;
        }

        public IEnumerable<FloatMenuOption> MakeOptions()
        {
            List<Thing> items = AvailableInjectors().ToList();
            if (items.Count == 0)
            {
                yield return new FloatMenuOption("No injectors in inventory", null);
                yield break;
            }
            foreach (Thing item in items)
            {
                Thing selectedItem = item;
                string label = item.LabelCap + " x" + item.stackCount + " — overdose " + (NextRisk * 100f).ToString("0.#") + "%";
                yield return new FloatMenuOption(label, () => RequestInjection(selectedItem));
            }
        }

        public void OpenMenu()
        {
            Find.WindowStack.Add(new FloatMenu(MakeOptions().ToList()));
        }

        private void RequestInjection(Thing item)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Downed || pawn.Dead || !AvailableInjectors().Contains(item)) return;
            bool fatal = IsObdolbosActive() || item.TryGetComp<CompInjector>().InjectorProps.originalObdolbos;
            if (NextRisk <= 0f) { QueueInjection(item); return; }
            string warning = pawn.LabelShortCap + " has stimulant load " + stimLoad + ".\n\n" +
                "Next injection overdose chance: " + (NextRisk * 100f).ToString("0.#") + "%\n" +
                (fatal ? "WARNING: Obdolbos interaction. An overdose will cause IMMEDIATE DEATH." :
                         "An overdose causes a life-threatening, treatable medical emergency.") +
                "\n\nAdminister " + item.LabelCap + "?";
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(warning, () => QueueInjection(item)));
        }

        private void QueueInjection(Thing item)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.jobs == null || !AvailableInjectors().Contains(item)) return;
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("TCI_InjectSelf"), item);
            pawn.jobs.TryTakeOrderedJob(job);
        }

        public void CompleteInjection(Thing item, Pawn administrator = null)
        {
            Pawn pawn = Pawn;
            CompInjector injection = item == null ? null : item.TryGetComp<CompInjector>();
            Pawn source = administrator ?? pawn;
            if (pawn == null || pawn.Dead || (pawn.Downed && administrator == null) || injection == null ||
                source == null || source.inventory == null || !source.inventory.innerContainer.Contains(item)) return;
            bool fatal = IsObdolbosActive() || injection.InjectorProps.originalObdolbos;
            HediffDef effect = injection.InjectorProps.effect;
            if (effect == null) return;

            // Consume exactly one syringe before rolling; keep the label before destroying it.
            string injectorName = item.LabelCap;
            item.SplitOff(1).Destroy(DestroyMode.Vanish);
            stimLoad++;
            nextDecayTick = Find.TickManager.TicksGame + DecayInterval;
            float rollRisk = Risks[Math.Min(stimLoad - 1, 4)];
            if (Rand.Value < rollRisk)
            {
                if (fatal)
                {
                    Messages.Message(pawn.LabelShortCap + " died from a fatal Obdolbos stimulant interaction.", MessageTypeDefOf.NegativeEvent);
                    pawn.Kill(null);
                    return;
                }
                ApplyOverdose(pawn);
                Messages.Message(pawn.LabelShortCap + " collapsed from acute stimulant overdose! Rescue and tend immediately.", MessageTypeDefOf.ThreatBig);
                return; // No beneficial effect from a failed injection.
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(effect);
            if (existing != null) pawn.health.RemoveHediff(existing); // Refresh, never stack duplicates.
            Hediff applied = pawn.health.AddHediff(effect);
            if (applied is Hediff_InjectorEffect active) active.OnInjected();
            if (injection.InjectorProps.randomObdolbos)
            {
                applied.Severity = Rand.Range(0, 3) + 0.1f; // randomized stage
                // No second unadvertised death roll: the shared stimulant risk controls overdoses.
            }
            Messages.Message(pawn.LabelShortCap + " injected " + injectorName + ". Stim load: " + stimLoad + ".", MessageTypeDefOf.NeutralEvent);
        }

        private bool IsObdolbosActive()
        {
            return Pawn != null && Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("TCI_Effect_Obdolbos")) != null;
        }
        private static void ApplyOverdose(Pawn pawn)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamed("TCI_AcuteOverdose");
            if (pawn.health.hediffSet.GetFirstHediffOfDef(def) == null)
                pawn.health.AddHediff(def).Severity = 0.6f;
        }
    }

    public class JobDriver_InjectPatient : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(180);
            Toil apply = new Toil();
            apply.initAction = () =>
            {
                Pawn patient = job.targetA.Thing as Pawn;
                if (patient == null || patient.Dead || pawn.Dead || pawn.Downed) return;
                CompInjectorController controller = patient.TryGetComp<CompInjectorController>();
                if (controller != null) controller.CompleteInjection(job.targetB.Thing, pawn);
            };
            apply.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return apply;
        }
    }

    public class JobDriver_InjectSelf : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) { return true; }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.Wait(120); // about 2 real seconds at normal speed
            Toil apply = new Toil();
            apply.initAction = () =>
            {
                CompInjectorController controller = pawn.TryGetComp<CompInjectorController>();
                if (controller != null && !pawn.Downed) controller.CompleteInjection(job.targetA.Thing);
            };
            apply.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return apply;
        }
    }
}
