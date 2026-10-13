using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TarkovInjectors
{
    // RimWorld 1.6 automatically discovers FloatMenuOptionProvider subclasses.
    // This only adds player-ordered pickup for *our* injector items. The built-in
    // TakeInventory job moves the items to inventory and checks encumbrance.
    public class FloatMenuOptionProvider_InjectorPickup : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            return pawn != null && pawn.IsColonistPlayerControlled && pawn.Spawned &&
                   !pawn.Dead && !pawn.Downed && pawn.inventory != null &&
                   pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0f;
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (clickedThing == null || !clickedThing.Spawned || clickedThing.stackCount <= 0 ||
                clickedThing.TryGetComp<CompInjector>() == null)
                yield break;

            if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                yield return new FloatMenuOption("Cannot pick up " + clickedThing.Label + ": no path", null);
                yield break;
            }
            if (!pawn.CanReserve(clickedThing))
            {
                yield return new FloatMenuOption("Cannot pick up " + clickedThing.Label + ": reserved", null);
                yield break;
            }

            int maxCount = Math.Min(clickedThing.stackCount,
                MassUtility.CountToPickUpUntilOverEncumbered(pawn, clickedThing));
            if (maxCount <= 0)
            {
                yield return new FloatMenuOption("Cannot pick up " + clickedThing.Label + ": too heavy", null);
                yield break;
            }

            if (clickedThing.stackCount == 1)
            {
                yield return PickupOption(pawn, clickedThing, 1, "Pick up " + clickedThing.LabelNoCount + " to inventory");
                yield break;
            }

            if (maxCount == clickedThing.stackCount)
            {
                yield return PickupOption(pawn, clickedThing, clickedThing.stackCount,
                    "Pick up all " + clickedThing.LabelNoCount + " (" + clickedThing.stackCount + ")");
            }
            else
            {
                yield return new FloatMenuOption("Cannot pick up all " + clickedThing.LabelNoCount + ": too heavy", null);
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("Pick up some " + clickedThing.LabelNoCount + "...", () =>
                {
                    if (!CanPickUp(pawn, clickedThing)) return;
                    int currentMax = Math.Min(clickedThing.stackCount,
                        MassUtility.CountToPickUpUntilOverEncumbered(pawn, clickedThing));
                    if (currentMax <= 0) return;
                    Find.WindowStack.Add(new Dialog_Slider(
                        "Pick up " + clickedThing.LabelNoCount + " to inventory", 1, currentMax,
                        count => SchedulePickup(pawn, clickedThing, count)));
                }, MenuOptionPriority.High), pawn, clickedThing);
        }

        private static FloatMenuOption PickupOption(Pawn pawn, Thing item, int count, string label)
        {
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, () => SchedulePickup(pawn, item, count), MenuOptionPriority.High),
                pawn, item);
        }

        private static bool CanPickUp(Pawn pawn, Thing item)
        {
            return pawn != null && pawn.IsColonistPlayerControlled && pawn.Spawned &&
                   !pawn.Dead && !pawn.Downed && pawn.inventory != null && pawn.jobs != null &&
                   pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0f &&
                   item != null && item.Spawned && item.stackCount > 0 &&
                   item.TryGetComp<CompInjector>() != null &&
                   pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly) &&
                   pawn.CanReserve(item);
        }

        private static void SchedulePickup(Pawn pawn, Thing item, int requestedCount)
        {
            if (!CanPickUp(pawn, item)) return;
            int maxCount = Math.Min(item.stackCount,
                MassUtility.CountToPickUpUntilOverEncumbered(pawn, item));
            if (requestedCount < 1 || requestedCount > maxCount) return;

            // An explicit player pickup order is allowed even if the stack was forbidden.
            item.SetForbidden(false, false);
            Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
            job.count = requestedCount;
            job.checkEncumbrance = true;
            job.takeInventoryDelay = 120;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
