using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Entities.Cards; // For PileType
using LimbusCore.LimbusCoreCode.Cards; // For ILimbusSpCostCard
using Godot; // For Texture2D, ResourceLoader, GD.Print
using System.Reflection; // For AccessTools.Field, MethodBase
using System; // For Type

namespace LimbusCore.LimbusCoreCode.Patches;

public static class SpCostVisualPatch
{
    private static Texture2D? _spIconTexture;

    [HarmonyPatch]
    public static class UpdateStarCostVisualsPatch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NCard), "UpdateStarCostVisuals", new Type[] { typeof(PileType) });
        }

        [HarmonyPostfix]
        public static void Postfix(NCard __instance, PileType pileType)
        {
            if (__instance.Model is ILimbusSpCostCard limbusCard && limbusCard.SpCost > 0)
            {
                if (_spIconTexture == null)
                {
                    _spIconTexture = ResourceLoader.Load<Texture2D>("res://LimbusCore/images/ui/combat/sp_icon.png");
                    if (_spIconTexture == null)
                    {
                        return;
                    }
                }

                var starIconField = AccessTools.Field(typeof(NCard), "_starIcon");
                if (starIconField != null)
                {
                    TextureRect? starIcon = starIconField.GetValue(__instance) as TextureRect;
                    if (starIcon != null)
                    {
                        starIcon.Texture = _spIconTexture;
                    }
                }
            }
        }
    }
}

            
        
    
