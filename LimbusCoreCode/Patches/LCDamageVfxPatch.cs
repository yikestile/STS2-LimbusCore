using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Godot;
using LimbusCore.LimbusCoreCode.Mechanics;
using MegaCrit.Sts2.addons.mega_text;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create), typeof(Creature), typeof(int), typeof(bool))]
public static class LCDamageVfxPatch
{
    private static readonly string CritImagePath = "res://LimbusCore/images/ui/combat/crit_icon.png";
    private static readonly string SlashImagePath = "res://LimbusCore/images/ui/combat/slash_icon.png";
    private static readonly string BluntImagePath = "res://LimbusCore/images/ui/combat/blunt_icon.png";
    private static readonly string PierceImagePath = "res://LimbusCore/images/ui/combat/pierce_icon.png";
    private static readonly string CustomFontPath = "res://LimbusCore/ExcelsiorSans.ttf";

    [HarmonyPostfix]
    public static void Postfix(NDamageNumVfx? __result, Creature target, int damage)
    {
        if (__result == null) return;

        ICombatState? combat = target.CombatState;
        if (combat == null) return;

        bool isCrit = combat.Creatures.Any(c => CritRegistry.WasLastAttackCrit[c]);
        LimbusDamageType type = DamageTypeTracker.LastDamageType[target];

        var label = __result.GetNodeOrNull<MegaLabel>("Label");
        float labelWidth = 0;

        if (label != null)
        {
            if (ResourceLoader.Exists(CustomFontPath))
            {
                var font = ResourceLoader.Load<Font>(CustomFontPath);
                if (font != null) label.AddThemeFontOverride("font", font);
            }

            label.AddThemeConstantOverride("outline_size", 8); 

            if (isCrit)
            {
                label.Modulate = new Color("f7be00");
                label.AddThemeColorOverride("font_outline_color", new Color("ff5d00"));
            }
            else
            {
                label.Modulate = new Color("f9e8c2");
                label.AddThemeColorOverride("font_outline_color", new Color("372f23"));
            }

            // Get the width of the rendered text to position the icon dynamically
            // Standard Godot Label uses GetMinimumSize() or the font's GetStringSize()
            var usedFont = label.GetThemeFont("font");
            int fontSize = label.GetThemeFontSize("font_size");
            labelWidth = usedFont.GetStringSize(label.Text, HorizontalAlignment.Left, -1, fontSize).X;
        }

        if (type != LimbusDamageType.None)
        {
            string path = type switch
            {
                LimbusDamageType.Slash => SlashImagePath,
                LimbusDamageType.Blunt => BluntImagePath,
                LimbusDamageType.Pierce => PierceImagePath,
                _ => ""
            };
            
            // Calculate X offset: Half of the label width + fixed padding (e.g., -40)
            float xOffset = -(labelWidth / 2f + 40f);
            AddIcon(__result, path, new Vector2(xOffset, 10));
        }

        if (isCrit)
        {
            AddIcon(__result, CritImagePath, new Vector2(0, 50));
        }
    }

    private static void AddIcon(NDamageNumVfx vfxNode, string path, Vector2 offset)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;
        
        var sprite = new Sprite2D();
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
        sprite.Scale = new Vector2(0.5f, 0.5f);
        sprite.Position = offset;
        vfxNode.AddChild(sprite);
    }
}