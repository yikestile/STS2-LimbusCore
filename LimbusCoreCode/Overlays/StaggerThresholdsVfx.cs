using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using LimbusCore.LimbusCoreCode.Mechanics; 
using LimbusCore.LimbusCoreCode.Powers; 
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Reflection; 
using MegaCrit.Sts2.Core.Combat; 
using System.Linq; 

namespace LimbusCore.LimbusCoreCode.Overlays;

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
public static class StaggerThresholdsVfx
{
    private static readonly AccessTools.FieldRef<NHealthBar, Creature> _creatureField =
        AccessTools.FieldRefAccess<NHealthBar, Creature>("_creature");

    private static readonly PropertyInfo _maxFgWidthProperty = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
    private static readonly MethodInfo _getMaxFgWidthMethod = _maxFgWidthProperty?.GetGetMethod(true);

    private static readonly ConditionalWeakTable<NHealthBar, List<Line2D>> _thresholdLines = new();

    private static readonly Color StaggerPlusPlusColor = new Color("04dc9b");
    private static readonly Color StaggerPlusColor = new Color("bd0000");
    private static readonly Color StaggerColor = new Color("e3c63d");
    private const float LineWidth = 4f;

    [HarmonyPostfix]
    public static void Postfix(NHealthBar __instance)
    {
        Creature creature = _creatureField(__instance);
        if (creature == null) return;

        TremorMain tremorPower = creature.GetPower<TremorMain>();
        
        if (!_thresholdLines.TryGetValue(__instance, out List<Line2D> lines))
        {
            lines = new List<Line2D>();
            _thresholdLines.Add(__instance, lines);
        }

        foreach (var line in lines)
        {
            line.QueueFree();
        }
        lines.Clear();

        if (tremorPower != null)
        {
            float maxHp = creature.MaxHp;
            float currentHp = creature.CurrentHp;
            float potencyAdjustment = tremorPower.Potency / 100f;

            int thresholdsReached = tremorPower.DynamicVars["ThresholdsReached"].IntValue;

            Control hpBarContainer = __instance.HpBarContainer;
            float hpBarWidth = 0f;
            if (_getMaxFgWidthMethod != null)
            {
                hpBarWidth = (float)_getMaxFgWidthMethod.Invoke(__instance, null);
            }
            
            if (hpBarWidth <= 0) return;
            float hpBarHeight = hpBarContainer.Size.Y;

            for (int k = 1; k <= 3; k++)
            {
                int visualLevel = k - thresholdsReached;

                if (visualLevel <= 0) continue;

                float basePerc = k switch { 1 => 0.75f, 2 => 0.50f, 3 => 0.25f, _ => 0f };
                float hpThreshold = maxHp * Mathf.Clamp(basePerc + potencyAdjustment, 0f, 1f);

                if (currentHp <= hpThreshold) continue;

                Color lineColor = visualLevel switch
                {
                    1 => StaggerColor,
                    2 => StaggerPlusColor,
                    3 => StaggerPlusPlusColor,
                    _ => StaggerColor
                };

                AddThresholdLine(__instance, lines, hpBarContainer, hpBarWidth, hpBarHeight, hpThreshold, maxHp, lineColor);
            }
        }
    }

    private static void AddThresholdLine(NHealthBar healthBar, List<Line2D> lines, Control parent, float hpBarWidth, float hpBarHeight, float thresholdHp, float maxHp, Color color)
    {
        if (thresholdHp <= 0 || thresholdHp >= maxHp || hpBarWidth <= 0) return; 

        float xPosition = (thresholdHp / maxHp) * hpBarWidth;

        float heightExtension = 6f; 

        Line2D line = new Line2D
        {
            Name = $"StaggerThresholdLine_{thresholdHp}",
            DefaultColor = color,
            Width = LineWidth,
            Points = new Vector2[] { 
                new Vector2(xPosition, -heightExtension), 
                new Vector2(xPosition, hpBarHeight + heightExtension) 
            },
            ZIndex = 0 
        };
        parent.AddChild(line);
        lines.Add(line);
    }
}