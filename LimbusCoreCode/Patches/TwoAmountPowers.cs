using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models;
using System;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch(typeof(NPower), "RefreshAmount")]
public static class TwoAmountPowers
{
    private static readonly AccessTools.FieldRef<NPower, MegaLabel> AmountLabelField = 
        AccessTools.FieldRefAccess<NPower, MegaLabel>("_amountLabel");

    [HarmonyPostfix]
    public static void Postfix(NPower __instance)
    {
        if (!__instance.IsNodeReady()) return;
        
        var model = __instance.Model;
        if (model is IHasSecondAmount hasSecondAmount)
        {
            var amountLabel = AmountLabelField(__instance);
            if (amountLabel == null) return;

            var amount2Label = __instance.GetNodeOrNull<MegaLabel>("Amount2Label");
            if (amount2Label == null)
            {
                amount2Label = (MegaLabel)amountLabel.Duplicate();
                amount2Label.Name = "Amount2Label";
                __instance.AddChild(amount2Label);

                amount2Label.Position = amountLabel.Position + new Vector2(0, -22); 
            }

            string secondAmount = hasSecondAmount.GetSecondAmount();
            amount2Label.SetTextAutoSize(secondAmount);
            amount2Label.Visible = !string.IsNullOrEmpty(secondAmount);
            
            amount2Label.AddThemeColorOverride("font_color", model.AmountLabelColor);
        }
        else
        {
            var amount2Label = __instance.GetNodeOrNull<MegaLabel>("Amount2Label");
            if (amount2Label != null)
            {
                amount2Label.Visible = false;
            }
        }
    }
}
