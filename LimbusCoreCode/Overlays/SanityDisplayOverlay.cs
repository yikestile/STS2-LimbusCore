using Godot;
using HarmonyLib;
using LimbusCore.LimbusCoreCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core;
using System.Linq;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LimbusCore.LimbusCoreCode.Overlays;

public partial class SanityDisplayOverlay : Control
{
    public static SanityDisplayOverlay? Instance { get; private set; }

    private NSanityDisplay? _sanityDisplay; 

    public override void _Ready()
    {
        Instance = this;
        Name = "SanityDisplayOverlay";
        
        MouseFilter = MouseFilterEnum.Ignore; 

        var sanityDisplayScene = GD.Load<PackedScene>("res://LimbusCore/scenes/SanityDisplay.tscn");
        if (sanityDisplayScene != null)
        {
            _sanityDisplay = sanityDisplayScene.Instantiate<NSanityDisplay>();
            AddChild(_sanityDisplay);
            
            // Allow input on the inner display so it can show its own tooltip
            _sanityDisplay.MouseFilter = MouseFilterEnum.Stop;
            _sanityDisplay.CustomMinimumSize = new Vector2(128, 128); 
            
            _sanityDisplay.SetAnchorsPreset(LayoutPreset.BottomLeft);
            _sanityDisplay.Position = new Vector2(-70, -60); 
            
            var currentState = CombatManager.Instance.DebugOnlyGetState();
            if (currentState != null)
            {
                SetupDisplay(currentState);
            }
            
            CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
    }

    public void HideDisplay()
    {
        if (_sanityDisplay != null)
        {
            _sanityDisplay.Visible = false;
        }
    }

    private void OnCombatSetUp(CombatState state)
    {
        SetupDisplay(state);
    }

    private void SetupDisplay(CombatState? combatState)
    {
        if (_sanityDisplay == null) return;
        _sanityDisplay.Visible = true;
        
        if (combatState != null)
        {
             var player = combatState.Players.FirstOrDefault(p => LocalContext.IsMe(p));
             if (player != null)
             {
                 _sanityDisplay.Initialize(player);
             }
        }
    }
}

[HarmonyPatch(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready))]
public static class SanityDisplayOverlayPatch
{
    public static void Postfix(NEnergyCounter __instance)
    {
        if (SanityDisplayOverlay.Instance != null) return;

        var overlay = new SanityDisplayOverlay();
        __instance.AddChild(overlay);
    }
}