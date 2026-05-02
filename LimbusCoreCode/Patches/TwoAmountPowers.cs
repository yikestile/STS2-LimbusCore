using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Models; // For PowerModel
using System;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch(typeof(NPower), "RefreshAmount")]
public static class TwoAmountPowers
{
    private static readonly AccessTools.FieldRef<NPower, MegaLabel> AmountLabelField = 
        AccessTools.FieldRefAccess<NPower, MegaLabel>("_amountLabel");
    
    // Access the private _model field directly for robustness
    private static readonly AccessTools.FieldRef<NPower, PowerModel> ModelField =
        AccessTools.FieldRefAccess<NPower, PowerModel>("_model");

    [HarmonyPostfix]
    public static void Postfix(NPower __instance)
    {
        if (!__instance.IsNodeReady()) return;
        
        // Access the private _model field directly
        var model = ModelField(__instance);
        if (model == null) return; // Ensure the model is set

        if (model is IHasSecondAmount hasSecondAmount)
        {
            var amountLabel = AmountLabelField(__instance);
            if (amountLabel == null) return;

            // Try to get existing label or create a new one
            var amount2Label = __instance.GetNodeOrNull<MegaLabel>("Amount2Label");
            if (amount2Label == null)
            {
                amount2Label = (MegaLabel)amountLabel.Duplicate();
                amount2Label.Name = "Amount2Label";
                amount2Label.UniqueNameInOwner = false; // Important for duplicated nodes
                __instance.AddChild(amount2Label);
                // Move it to be near the original amount label, assuming it's a sibling
                __instance.MoveChild(amount2Label, amountLabel.GetIndex());
            }

            string secondAmount = hasSecondAmount.GetSecondAmount();
            amount2Label.SetTextAutoSize(secondAmount);
            amount2Label.Visible = !string.IsNullOrEmpty(secondAmount);
            
            // Position the second amount label relative to the first
            var fontSize = amountLabel.GetThemeFontSize(ThemeConstants.Label.FontSize);
            amount2Label.Position = amountLabel.Position + new Vector2(0, -(fontSize + 2));
            
            amount2Label.AddThemeColorOverride("font_color", model.AmountLabelColor);
        }
        else
        {
            // If it's not a two-amount power, ensure the second label is hidden/removed
            var amount2Label = __instance.GetNodeOrNull<MegaLabel>("Amount2Label");
            if (amount2Label != null)
            {
                amount2Label.Visible = false;
            }
        }
    }
}
