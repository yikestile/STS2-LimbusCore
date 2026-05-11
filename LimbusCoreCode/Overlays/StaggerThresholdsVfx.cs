using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using LimbusCore.LimbusCoreCode.Mechanics;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace LimbusCore.LimbusCoreCode.Overlays;

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
public static class StaggerThresholdsVfx
{
private static readonly AccessTools.FieldRef<NHealthBar, Creature> _creatureField =
AccessTools.FieldRefAccess<NHealthBar, Creature>("_creature");

    private static readonly PropertyInfo _maxFgWidthProperty = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
    private static readonly MethodInfo _getMaxFgWidthMethod = _maxFgWidthProperty?.GetGetMethod(true);

    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold1Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold1");
    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold2Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold2");
    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold3Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold3");
    private static readonly AccessTools.FieldRef<TremorMain, int> _staggerAppliedTurnField =
        AccessTools.FieldRefAccess<TremorMain, int>("_staggerAppliedTurn");


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
            
            int raisedHp1 = _raisedHpThreshold1Field(tremorPower);
            int raisedHp2 = _raisedHpThreshold2Field(tremorPower);
            int raisedHp3 = _raisedHpThreshold3Field(tremorPower);

            float hpThreshold1 = (maxHp * TremorMain.STAGGER_THRESHOLD_1_PERCENT) + raisedHp1;
            float hpThreshold2 = (maxHp * TremorMain.STAGGER_THRESHOLD_2_PERCENT) + raisedHp2;
            float hpThreshold3 = (maxHp * TremorMain.STAGGER_THRESHOLD_3_PERCENT) + raisedHp3;

            Control hpBarContainer = __instance.HpBarContainer;
            float hpBarWidth = 0f;
            if (_getMaxFgWidthMethod != null)
            {
                hpBarWidth = (float)_getMaxFgWidthMethod.Invoke(__instance, null);
            }
            
            if (hpBarWidth <= 0) return;
            float hpBarHeight = hpBarContainer.Size.Y;

            int staggerAppliedTurn = _staggerAppliedTurnField(tremorPower);
            int currentCombatTurn = creature.CombatState?.RoundNumber ?? 0;

            var activeThresholds = new List<float>();
            if (currentHp > hpThreshold1) activeThresholds.Add(hpThreshold1);
            if (currentHp > hpThreshold2) activeThresholds.Add(hpThreshold2);
            if (currentHp > hpThreshold3) activeThresholds.Add(hpThreshold3);

            activeThresholds = activeThresholds.OrderByDescending(t => t).ToList();

            for (int i = 0; i < activeThresholds.Count; i++)
            {
                Color lineColor;
                if (currentCombatTurn == staggerAppliedTurn)
                {
                    float originalThresholdHp = activeThresholds[i];
                    
                    if (originalThresholdHp == hpThreshold3) lineColor = StaggerPlusPlusColor;
                    else if (originalThresholdHp == hpThreshold2) lineColor = StaggerPlusColor;
                    else if (originalThresholdHp == hpThreshold1) lineColor = StaggerColor;
                    else lineColor = StaggerColor;
                }
                else
                {
                    switch (i)
                    {
                        case 0: lineColor = StaggerColor; break;
                        case 1: lineColor = StaggerPlusColor; break;
                        case 2: lineColor = StaggerPlusPlusColor; break;
                        default: lineColor = StaggerColor; break;
                    }
                }
                AddThresholdLine(__instance, lines, hpBarContainer, hpBarWidth, hpBarHeight, activeThresholds[i], maxHp, lineColor);
            }
        }
    }

    private static void AddThresholdLine(NHealthBar healthBar, List<Line2D> lines, Control parent, float hpBarWidth, float hpBarHeight, float thresholdHp, float maxHp, Color color)
    {
        if (thresholdHp <= 0 || thresholdHp >= maxHp || hpBarWidth <= 0) return; 

        float xPosition = (thresholdHp / maxHp) * hpBarWidth;
        float heightExtension = 6f; 
        float strokeSize = 2f;

        Line2D strokeLine = new Line2D
        {
            Name = $"StaggerStroke_{thresholdHp}",
            DefaultColor = new Color(0, 0, 0, 1),
            Width = LineWidth + (strokeSize * 2f),
            Points = new Vector2[] { 
                new Vector2(xPosition, -heightExtension), 
                new Vector2(xPosition, hpBarHeight + heightExtension) 
            },
            ZIndex = 0 
        };
        parent.AddChild(strokeLine);
        lines.Add(strokeLine);

        Line2D mainLine = new Line2D
        {
            Name = $"StaggerLine_{thresholdHp}",
            DefaultColor = color,
            Width = LineWidth,
            Points = new Vector2[] { 
                new Vector2(xPosition, -heightExtension), 
                new Vector2(xPosition, hpBarHeight + heightExtension) 
            },
            ZIndex = 1 
        };
        parent.AddChild(mainLine);
        lines.Add(mainLine);
    }
}