# Tarkov Combat Injectors — RimWorld

**Uncompiled development prototype (v0.1)** targeting **RimWorld 1.6**.

An unofficial RimWorld mod prototype adapting 22 Escape from Tarkov-style tactical injectors into colony medicine, combat, and survival. This repository contains the **source files and original placeholder syringe artwork**, but **does not yet contain a compiled DLL**. Do not install it into an important save until it compiles and passes in-game testing.

## Features included in the source

- **22 injectors**, each with an individual recipe, research tier, duration, and temporary health effect
- **Manual carrying and activation only.** Select a pawn, right-click an injector lying on the map, and use **Pick up to inventory** (or **Pick up some** for stacked syringes). Select the pawn and open **Inject** to use a carried syringe. Pawns never take injectors for recreation, drug policies, injury, or combat without an explicit player order.
- **Doctor administration prototype** for downed patients by player order
- **Shared escalating overdose odds** per injection: 1st **0%**, 2nd **20%**, 3rd **40%**, 4th **80%**, and 5th or later **99.5%**
- **Acute overdose:** Pawn collapses with a tendable, worsening medical condition; doctor treatment can stabilize and eventually recover the pawn
- **Original Obdolbos:** Any triggered overdose while it is active or being injected is **instantly fatal**. Obdolbos 2 does not share that exception
- **Drug Lab crafting**, with four research tiers, using vanilla RimWorld ingredients
- **Neutroamine remains vanilla:** Trade/loot only; no new production recipe
- **23 original placeholder PNG icons** (22 syringes + action button)

Stim load is persistent across saves, and decays by one stack after six in-game hours per interval. Drug effects refresh rather than multiply if repeated.

## Included injectors

Morphine, Adrenaline, Propital, eTG-change, PNB, P22, SJ1, SJ6, L1, Trimadol, Meldonin, 3-(b-TG), 2A2-(b-TG), Zagustin, AHF1-M, Perfotoran, xTG-12, SJ9, SJ12, M.U.L.E., Obdolbos, and Obdolbos 2.

## Project layout

```text
About/About.xml                        RimWorld mod metadata
Defs/Injectors.xml                     Items and crafting recipes
Defs/Effects.xml                       Duration, capacity, and drug effect definitions
Defs/MedicalConditions.xml             Overdose, exhaustion, chemical strain
Defs/Research.xml                      Four research tiers
Defs/Jobs.xml                          Self-injection and doctor injection jobs (pickup uses vanilla TakeInventory)
Patches/ColonistController.xml         Inject command for human pawns
Source/InjectorCore.cs                 Manual injection, overdose, and jobs
Source/InjectorHediffs.cs              Medical and drug effect behavior
Source/FloatMenuOptionProvider_InjectorPickup.cs  Manual syringe pickup context menu
Source/TarkovCombatInjectors.csproj    C# project (no DLL included)
Textures/TarkovInjectors/              Placeholder PNG textures
DesignData.json                        Injector specification data
Source_Validation.py                   Static XML/content checks
BUILD_WINDOWS.bat                      Local Windows build helper
```

## Building locally

1. Install the .NET SDK and **.NET Framework 4.7.2 targeting pack** needed by the source project.
2. Install RimWorld 1.6 and locate `RimWorldWin64_Data/Managed`.
3. Run:
   ```powershell
   dotnet build Source/TarkovCombatInjectors.csproj -c Release -p:RimWorldManaged="C:\Path\To\RimWorldWin64_Data\Managed"
   ```
4. Make sure `Assemblies/TarkovCombatInjectors.dll` was created successfully.
5. Copy the repository contents into a subfolder of `RimWorld/Mods`, enable it in the mod list, and test in a disposable colony.

You can run `python Source_Validation.py` to validate the basic source structure independently of RimWorld.

## Known prototype limitations

- **Not compiled or playtested.** C# calls and some XML defs may need correction against the RimWorld 1.6 game assemblies. The new manual pickup UI uses RimWorld 1.6's native `FloatMenuOptionProvider`, `JobDefOf.TakeInventory`, and inventory mass checks, but remains unverified in-game.
- SJ9 thermal stealth does not yet alter enemy targeting AI.
- M.U.L.E. and 2A2 use carrying-capacity stat modifiers; not every inventory/caravan mass calculation is covered.
- Bleeding prevention currently uses vanilla wound tending rather than a separate clotting hediff.
- Doctor administration and saving/loading all related states still need real-game verification.
- No official EFT imagery, sound, or game assemblies are redistributed.

## Project status

**v0.1 source prototype**, not an install-ready release. The next milestone is a successful compile against RimWorld 1.6, fixing any red errors, and practical overdose/injection testing.

This is an unofficial fan-made adaptation and is not affiliated with Battlestate Games or Ludeon Studios.
