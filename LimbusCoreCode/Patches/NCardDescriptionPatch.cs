using System;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Models;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace LimbusCore.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class NCardDescriptionPatch
{
    private static readonly AccessTools.FieldRef<NCard, MegaRichTextLabel> DescriptionLabelField = 
        AccessTools.FieldRefAccess<NCard, MegaRichTextLabel>("_descriptionLabel");

    private static bool IsLimbusCard(CardModel card)
    {
        if (card?.Id?.Entry == null) return false;
    
        var id = card.Id.Entry;
        return id.Contains("RienSang") || id.Contains("Limbus");
    }

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (!IsLimbusCard(__instance.Model)) return;

        Callable.From(() => ApplyLimbusSizing(__instance)).CallDeferred();
    }

    public static void ApplyLimbusSizing(NCard card)
    {
        try 
        {
            MegaRichTextLabel label = DescriptionLabelField(card);
            if (label != null)
            {
                label.FitContent = false;
                label.AutoSizeEnabled = true;
                label.IsVerticallyBound = true;
                label.MinFontSize = 6; 
                
                string currentLang = LocManager.Instance.Language;
                bool isCJK = currentLang == "jpn" || currentLang == "zhs" || currentLang == "zht" || currentLang == "kor";

                int textLength = label.Text.Length;
                int separation = 0;

                // Adjust thresholds for CJK languages which use fewer characters for more information
                if (isCJK)
                {
                    if (textLength > 60) separation = -4;
                    else if (textLength > 30) separation = -2;
                }
                else
                {
                    if (textLength > 300) separation = -4;
                    else if (textLength > 180) separation = -2;
                }

                if (separation != 0)
                    label.AddThemeConstantOverride("line_separation", separation);
                else
                    label.RemoveThemeConstantOverride("line_separation");

                // Force re-triggering of auto-size logic
                label.Call("AdjustFontSize");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[LimbusCore] Visual Patch Failed: {e.Message}");
        }
    }
}