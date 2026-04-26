using System.Threading.Tasks;
using HarmonyLib;
using LimbusCore.LimbusCoreCode.Mechanics;
using LimbusCore.LimbusCoreCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch]
public static class OnEvadeHookPatch
{
    [HarmonyPatch(typeof(LCEvadePower), nameof(LCEvadePower.ModifyHpLostBeforeOsty))]
    [HarmonyPostfix]
    public static void EvadePowerTriggerPostfix(LCEvadePower __instance, decimal __result)
    {
        if (__result == 0m && __instance.Owner != null)
        {
            var creature = __instance.Owner;
            int energyToGrant = EvadeRegistry.PendingEnergy[creature];
            
            if (energyToGrant > 0)
            {
                TaskHelper.RunSafely(PowerCmd.Apply<EnergyNextTurnPower>(new ThrowingPlayerChoiceContext(), creature, (decimal)energyToGrant, creature, null));
                EvadeRegistry.PendingEnergy[creature] = 0;
            }
        }
    }


    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
    [HarmonyPostfix]
    public static void BeforeSideTurnStartPostfix(ICombatState combatState, CombatSide side)
    {
        if (side != CombatSide.Player) return;
        
        TaskHelper.RunSafely(ProcessOnEvadeRewards(combatState));
    }

    private static async Task ProcessOnEvadeRewards(ICombatState combatState)
    {
        foreach (var player in combatState.Players)
        {
            var creature = player.Creature;
            if (EvadeRegistry.EvadeEffectStacks[creature] > 0)
            {
                if (LCEvadePower.HasEvadedThisTurn[creature])
                {
                    int potency = EvadeRegistry.PendingPoisePotency[creature];
                    int count = EvadeRegistry.PendingPoiseCount[creature];
                    if (count > 0)
                    {
                        await LCPoisePower.Apply(new ThrowingPlayerChoiceContext(), creature, count, potency, creature, null);
                    }

                    int draw = EvadeRegistry.PendingDraw[creature];
                    if (draw > 0)
                    {
                        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), (decimal)draw, player);
                    }

                    int sinkingPotency = EvadeRegistry.PendingSinkingPotencyAll[creature];
                    if (sinkingPotency > 0)
                    {
                        foreach (var enemy in combatState.Enemies)
                        {
                            if (enemy.IsAlive)
                            {
                                await LCSinkingPower.Apply(new ThrowingPlayerChoiceContext(), enemy, 1, sinkingPotency, creature, null);
                            }
                        }
                    }
                }

                EvadeRegistry.EvadeEffectStacks[creature] = 0;
                EvadeRegistry.PendingPoisePotency[creature] = 0;
                EvadeRegistry.PendingPoiseCount[creature] = 0;
                EvadeRegistry.PendingEnergy[creature] = 0;
                EvadeRegistry.PendingDraw[creature] = 0;
                EvadeRegistry.PendingSinkingPotencyAll[creature] = 0;
            }
        }
    }
}