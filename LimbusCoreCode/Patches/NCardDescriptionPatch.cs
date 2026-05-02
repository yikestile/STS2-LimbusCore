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
        if (card == null || card.Id?.Entry == null) return false;

        string id = card.Id.Entry;
        return id.Contains("RienSang", StringComparison.OrdinalIgnoreCase) || 
               id.Contains("Limbus", StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (__instance == null || __instance.Model == null) return;

        if (__instance.Model.IsCanonical)
        {
            return;
        }

        if (!IsLimbusCard(__instance.Model)) return;

        if (__instance.IsInsideTree())
        {
            Callable.From(() => ApplyLimbusSizing(__instance)).CallDeferred();
        }
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

                label.Call("AdjustFontSize");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[LimbusCore] Visual Patch Failed: {e.Message}");
        }
    }
}