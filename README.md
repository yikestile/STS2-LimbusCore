# STS2-LimbusCore
A core library and visual engine designed to support my current and future Limbus Company character mods in StS 2.

Currently required for The Index Nursefather Yi Sang mod.

## Main features:

- Sanity System (SP): A fully functional SP resource (ranging from -45 to 45) with unique mechanics and risk/reward thresholds.

- Status Library: Shared logic for Limbus specific mechanics: Poise, Sinking, Bleed, Burn, and more (supporting both Potency and Count).

- Damage Types: Introduces Slash, Pierce, and Blunt damage logic.

## Cinematic Visuals: 

- Limbus-style letterboxing with scrolling ink effects.
- Automatic UI suppression during cinematic sequences.
- Extended combat environments to allow for high-mobility animations.
- All options are toggleable.

## Sanity System (SP):
<img width="341" height="512" alt="image" src="https://github.com/user-attachments/assets/15a74194-58b4-4939-aaf3-00593e0efeaf" />

## Full Details:

- Basics: SP starts at 0 each combat. It is used as a resource for E.G.O cards.
- Scaling: Gain SP equal to 100% of damage dealt and a flat 5 SP per kill. Lose SP equal to 200% of damage received.
- Detriments: Between -1 and -40 SP, you have a chance to deal reduced damage.
  + Failure Chance: -(SP*3)% (e.g., -20 SP = 60% chance).
  + Damage Reduction: Current -(SP*1.5)% (e.g., -30 SP = 45% reduction).
- Panic: At -30 SP, the player is inflicted with a unique Panic debuff.
- Turn Skip: Between -35 and -45 SP, there is a chance to completely skip your turn (50% at -35 SP, 100% at -45 SP). After a skip, SP resets to 0.

## Requirements:

BaseLib: https://github.com/Alchyr/BaseLib-StS2/releases/
