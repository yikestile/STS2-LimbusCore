using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using LimbusCore.LimbusCoreCode.Mechanics;
using LimbusCore.LimbusCoreCode.Cards;
using System.Threading.Tasks;
using System;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch]
public static class SpCostPlayabilityPatch
{
    private const float MIN_SP = -45f;

    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.HasEnoughResourcesFor))]
    [HarmonyPrefix]
    public static bool HasEnoughResourcesPrefix(PlayerCombatState __instance, CardModel card, ref bool __result, ref UnplayableReason reason, Player ____player)
    {
        if (card is ILimbusSpCostCard limbusCard && limbusCard.SpCost > 0)
        {
            int energyRequired = Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
            float currentSp = SanityManager.GetSanity(____player);
            float spAfterCost = currentSp - limbusCard.SpCost;

            reason = UnplayableReason.None;
            if (energyRequired > __instance.Energy)
            {
                reason |= UnplayableReason.EnergyCostTooHigh;
            }
            if (spAfterCost < MIN_SP)
            {
                reason |= UnplayableReason.StarCostTooHigh;
            }

            __result = reason == UnplayableReason.None;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(CardModel), "SpendStars")]
    [HarmonyPrefix]
    public static bool SpendStarsPrefix(CardModel __instance, ref int amount)
    {
        if (__instance is ILimbusSpCostCard limbusCard && limbusCard.SpCost > 0)
        {
            if (amount > 0)
            {
                SanityManager.ModifySanity(__instance.Owner, -amount);
            }
            
            amount = 0;
            return true;
        }
        return true;
    }
}
