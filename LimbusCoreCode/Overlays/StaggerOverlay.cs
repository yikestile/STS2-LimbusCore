using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Reflection;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using LimbusCore.LimbusCoreCode.Powers; 
using System.Collections.Generic; 
using System.Runtime.CompilerServices; 

namespace LimbusCore.LimbusCoreCode.Overlays;

[HarmonyPatch]
public static class StaggerOverlay
{
    private class StaggerData 
    { 
        public int Level; 
    }

    private static readonly string _staggerIconPath = "res://LimbusCore/images/ui/combat/stagger.png";
    private static readonly string _staggerPlusIconPath = "res://LimbusCore/images/ui/combat/stagger+.png";
    private static readonly string _staggerPlusPlusIconPath = "res://LimbusCore/images/ui/combat/stagger++.png";

    private const string StaggerNodeName = "StaggerOverlay_Stagger";
    private const string StaggerPlusNodeName = "StaggerOverlay_StaggerPlus";
    private const string StaggerPlusPlusNodeName = "StaggerOverlay_StaggerPlusPlus";

    private static readonly ConditionalWeakTable<Creature, StaggerData> _highestStaggerLevelReached = new();

    [HarmonyPatch(typeof(NCreature), "_Ready")]
    [HarmonyPostfix]
    public static void AddStaggerIcons(NCreature __instance)
    {
        if (__instance.Entity == null) return;

        var staggerIcon = new TextureRect
        {
            Name = StaggerNodeName,
            Texture = ResourceLoader.Load<Texture2D>(_staggerIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        __instance.AddChild(staggerIcon);

        var staggerPlusIcon = new TextureRect
        {
            Name = StaggerPlusNodeName,
            Texture = ResourceLoader.Load<Texture2D>(_staggerPlusIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        __instance.AddChild(staggerPlusIcon);

        var staggerPlusPlusIcon = new TextureRect
        {
            Name = StaggerPlusPlusNodeName,
            Texture = ResourceLoader.Load<Texture2D>(_staggerPlusPlusIconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        __instance.AddChild(staggerPlusPlusIcon);
        
        UpdateStaggerOverlay(__instance);
    }

    [HarmonyPatch(typeof(NCreature), "UpdateBounds", typeof(Node))]
    [HarmonyPostfix]
    public static void UpdateIconPosition(NCreature __instance)
    {
        UpdateStaggerOverlayPosition(__instance, __instance.GetNodeOrNull<TextureRect>(StaggerNodeName));
        UpdateStaggerOverlayPosition(__instance, __instance.GetNodeOrNull<TextureRect>(StaggerPlusNodeName));
        UpdateStaggerOverlayPosition(__instance, __instance.GetNodeOrNull<TextureRect>(StaggerPlusPlusNodeName));
    }

    private static void UpdateStaggerOverlayPosition(NCreature node, TextureRect? iconNode)
    {
        if (iconNode == null) return;

        var hitbox = node.Hitbox;
        if (hitbox != null)
        {
            float size = Mathf.Min(hitbox.Size.X, hitbox.Size.Y) * 0.75f;
            iconNode.Size = new Vector2(size, size);
            iconNode.CustomMinimumSize = iconNode.Size;

            iconNode.Position = hitbox.Position + (hitbox.Size / 2f) - (iconNode.Size / 2f);
            iconNode.ZIndex = 1; 
        }
    }

    [HarmonyPatch(typeof(Creature), "ApplyPowerInternal")]
    [HarmonyPostfix]
    public static void OnPowerApplied(Creature __instance)
    {
        RefreshCreatureNode(__instance);
    }

    [HarmonyPatch(typeof(Creature), "RemovePowerInternal")]
    [HarmonyPostfix]
    public static void OnPowerRemoved(Creature __instance)
    {
        RefreshCreatureNode(__instance);
    }

    private static void RefreshCreatureNode(Creature entity)
    {
        if (NCombatRoom.Instance == null) return;
        var node = NCombatRoom.Instance.GetCreatureNode(entity);
        if (node != null)
        {
            UpdateStaggerOverlay(node);
        }
    }

    public static void UpdateStaggerOverlay(NCreature node)
    {
        if (node.Entity == null) return;

        var staggerPower = node.Entity.GetPower<LCStaggerPower>();
        var staggerIcon = node.GetNodeOrNull<TextureRect>(StaggerNodeName);
        var staggerPlusIcon = node.GetNodeOrNull<TextureRect>(StaggerPlusNodeName);
        var staggerPlusPlusIcon = node.GetNodeOrNull<TextureRect>(StaggerPlusPlusNodeName);

        if (staggerIcon != null) staggerIcon.Visible = false;
        if (staggerPlusIcon != null) staggerPlusIcon.Visible = false;
        if (staggerPlusPlusIcon != null) staggerPlusPlusIcon.Visible = false;

        if (staggerPower != null)
        {
            switch (staggerPower.StaggerLevel)
            {
                case 1:
                    if (staggerIcon != null) staggerIcon.Visible = true;
                    break;
                case 2:
                    if (staggerPlusIcon != null) staggerPlusIcon.Visible = true;
                    break;
                case 3:
                    if (staggerPlusPlusIcon != null) staggerPlusPlusIcon.Visible = true;
                    break;
            }

            UpdateStaggerOverlayPosition(node, staggerIcon);
            UpdateStaggerOverlayPosition(node, staggerPlusIcon);
            UpdateStaggerOverlayPosition(node, staggerPlusPlusIcon);
        }
    }
}