# Tarkov Combat Injectors — RimWorld

A work-in-progress mod bringing **22 Escape from Tarkov-inspired injectors** to RimWorld.

> **Status:** Development prototype (v0.1). Not compiled, not playtested, and **not yet installable**. This repository is being prepared for the mod source.

## Design

- **Manual injection only.** Pawns never use injectors for recreation, mood, addiction cravings, or automatically during combat.
- **Inventory-driven injection menu.** The player's Inject command lists syringes the selected pawn is carrying.
- **Compounding overdose risk.** First through fifth+ injections: **0%, 20%, 40%, 80%, 99.5%**.
- **Regular overdose:** A medical emergency causing collapse; treatment and recovery are possible.
- **Original Obdolbos:** If an overdose triggers while original Obdolbos is active or being injected, the pawn dies immediately.
- **Crafting:** Four research tiers, Drug Lab recipes using standard RimWorld ingredients.
- **Neutroamine:** Vanilla trade/loot supply only. No custom manufacturing recipe.
- **22 planned injectors:** Morphine, Adrenaline, Propital, eTG-change, PNB, P22, SJ1, SJ6, L1, Trimadol, Meldonin, 3-(b-TG), 2A2-(b-TG), Zagustin, AHF1-M, Perfotoran, xTG-12, SJ9, SJ12, M.U.L.E., Obdolbos, Obdolbos 2.

## Planned mod layout

```text
About/About.xml
Defs/
Patches/
Source/
Textures/TarkovInjectors/
Assemblies/             # compiled DLL, not yet built
```

## Requirements

- RimWorld 1.6 (target version)
- No Neutroamine production mod required

## Development status

The v0.1 source prototype requires compilation against the RimWorld/Unity game assemblies and in-game API testing. Do not treat a source package as a finished playable release. In particular, the special stealth, carrying capacity, bleeding treatment, and doctor-administration mechanics need integration testing.

## Credits

Original RimWorld adaptation and implementation developed for the Tarkov RimWorld Stimulants mod project. This project is unofficial and is not affiliated with Battlestate Games or Ludeon Studios. No extracted EFT game art is included.
