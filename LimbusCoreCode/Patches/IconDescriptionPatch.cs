using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using LimbusCore.LimbusCoreCode.Cards;
using MegaCrit.Sts2.Core.Localization;
using System.Collections.Generic;
using System.Reflection; // Required for MethodBase and MethodInfo
using System; // Required for Type
using Godot; // Required for GD.Print

namespace LimbusCore.LimbusCoreCode.Patches;

public static class IconDescriptionPatch
{
    private static readonly Dictionary<string, string> CustomIconReplacements = new()
    {
        { "[img]res://images/packed/sprite_fonts/star_icon.png[/img]", "[img]res://LimbusCore/images/ui/combat/sp_icon.png[/img]" },
    };

    [HarmonyPatch]
    public static class GetDescriptionForPilePatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", new Type[] { typeof(PileType), AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"), typeof(Creature) });
        }

        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, ref string __result)
        {
            
            if (__instance is ILimbusSpCostCard limbusCard && limbusCard.SpCost > 0)
            {
                if (CustomIconReplacements.TryGetValue("[img]res://images/packed/sprite_fonts/star_icon.png[/img]", out string spIconPath))
                {
                    if (__result.Contains("[img]res://images/packed/sprite_fonts/star_icon.png[/img]"))
                    {
                        __result = __result.Replace("[img]res://images/packed/sprite_fonts/star_icon.png[/img]", spIconPath);
                    }
                }
            }

            // 2. Handle other generic icon replacements
            foreach (var replacement in CustomIconReplacements)
            {
                if (replacement.Key == "[img]res://images/packed/sprite_fonts/star_icon.png[/img]" &&
                    !(__instance is ILimbusSpCostCard lc && lc.SpCost > 0))
                {
                    continue;
                }
                
                if (__result.Contains(replacement.Key))
                {
                    __result = __result.Replace(replacement.Key, replacement.Value);
                }
            }
        }
    }
}
