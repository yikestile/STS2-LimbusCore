//unused for now but kept in case it's needed in the future.
/*
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Models;

namespace LimbusCore.Patches
{
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    public static class CardDescriptionKeywordPatch
    {
        private static readonly AccessTools.FieldRef<NCard, MegaRichTextLabel> DescriptionLabelField = 
            AccessTools.FieldRefAccess<NCard, MegaRichTextLabel>("_descriptionLabel");

        private static readonly Regex KeywordRegex = new Regex(@"\[gold\].+?\[/gold\]\.", RegexOptions.Compiled);

        private static bool IsLimbusCard(CardModel card)
        {
            if (card?.Owner?.Character?.Id.Entry == null) return false;
            string id = card.Owner.Character.Id.Entry;
            return id.StartsWith("RienSang", StringComparison.OrdinalIgnoreCase) || 
                   id.StartsWith("Limbus", StringComparison.OrdinalIgnoreCase);
        }

        [HarmonyPostfix]
        public static void Postfix(NCard __instance)
        {
            try 
            {
                if (!IsLimbusCard(__instance.Model)) return;

                MegaRichTextLabel label = DescriptionLabelField(__instance);
                if (label == null || string.IsNullOrEmpty(label.Text)) return;

                string originalText = label.Text;

                var matches = KeywordRegex.Matches(originalText);
                if (matches.Count == 0) return;

                List<string> keywords = new List<string>();
                foreach (Match m in matches)
                {
                    keywords.Add(m.Value);
                }

                string bodyText = KeywordRegex.Replace(originalText, "").Trim();

                string keywordLine = string.Join(" ", keywords);

                string processedText = $"[center]{keywordLine}\n{bodyText}[/center]";

                if (processedText != originalText)
                {
                    label.SetTextAutoSize(processedText);
                }
            }
            catch (Exception)
            {
                // Prevent crashes
            }
        }
    }
}
*/