using Godot;
using HarmonyLib;
using LimbusCore.LimbusCoreCode.Mechanics;
using LimbusCore.LimbusCoreCode.Overlays;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LimbusCore.LimbusCoreCode.Powers; // For IPanicPower
using System;

namespace LimbusCore.LimbusCoreCode.Patches;

[HarmonyPatch]
public static class SanityPatches
{
    private static bool IsLimbusCharacter(Player player)
    {
        var modelId = player.Character.Id.Entry;
        return modelId.StartsWith("RienSang", StringComparison.OrdinalIgnoreCase) || modelId.StartsWith("Limbus", StringComparison.OrdinalIgnoreCase);
    }
    
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.Abandon))]
    [HarmonyPostfix]
    public static void OnRunAbandoned()
    {
        SanityDisplayOverlay.Instance?.HideDisplay();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageGiven))]
    [HarmonyPostfix]
    public static void AfterDamageGivenPatch(PlayerChoiceContext choiceContext, CombatState combatState, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress) return;
        
        if (dealer != null)
        {
            var player = dealer.Player; 
            if (player != null && IsLimbusCharacter(player))
            {
                if (results.UnblockedDamage > 0)
                {
                    var spGain = (float)Math.Floor(results.UnblockedDamage * 1f);

                    if (cardSource != null)
                    {
                        var prop = cardSource.GetType().GetProperty("IsEgoCard");
                        if (prop != null && prop.PropertyType == typeof(bool) && (bool?)prop.GetValue(cardSource) == true)
                        {
                            spGain = (float)Math.Floor(results.UnblockedDamage * 0.3f);
                        }
                    }

                    SanityManager.ModifySanity(player, spGain); 
                }
                if (results.WasTargetKilled)
                {
                    SanityManager.ModifySanity(player, 5f); 
                }
                
                CheckAndApplyPanic(player);
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
    [HarmonyPostfix]
    public static void LoseSPOnDamage(PlayerChoiceContext choiceContext, IRunState runState, CombatState? combatState, Creature? target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress) return; 
        if (target == null) return;

        var player = target.Player; 
        if (player == null || !IsLimbusCharacter(player)) return;

        var damageTaken = result.UnblockedDamage;
        if (damageTaken <= 0) return;
        
        var spLoss = (float)Math.Floor(damageTaken * 2f);
        SanityManager.ModifySanity(player, -spLoss); 

        CheckAndApplyPanic(player);
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
    [HarmonyPostfix]
    public static void ResetSanityOnPlayerTurnStart(CombatState combatState, CombatSide side)
    {
        if (side != CombatSide.Player) return;

        foreach (var player in combatState.Players)
        {
            if (player == null || !IsLimbusCharacter(player)) continue;

            var sanityData = SanityManager.GetData(player);
            if (sanityData.NeedsSanityResetAfterStun)
            {
                SanityManager.ResetSanity(player);
                sanityData.NeedsSanityResetAfterStun = false;
                
                RemovePanic(player);
            }
            
            CheckAndApplyPanic(player);
        }
    }

    private static bool ShouldSkipTurn(Player player)
    {
        var sp = SanityManager.GetSanity(player);
        if (sp > -35f) return false;
        var absSp = Math.Max(0f, -sp);
        var chance = (5f * absSp) - 125f;
        if (player.Creature.CombatState == null) return false;
        var roll = player.Creature.CombatState.RunState.Rng.Niche.NextFloat() * 100f;
        return roll < chance;
    }

    [HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
    [HarmonyPrefix]
    public static bool HandleTurnSkip(CombatManager __instance, Player player, HookPlayerChoiceContext playerChoiceContext, ref Task __result)
    {
        if (!IsLimbusCharacter(player)) return true; 
        if (!ShouldSkipTurn(player)) return true; 

        SanityManager.GetData(player).NeedsSanityResetAfterStun = true;
        
        var vfx = NStunnedVfx.Create(player.Creature);
        if (vfx != null)
        {
            Callable.From(() => { NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx); }).CallDeferred();
        }
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/ceremonial_beast/ceremonial_beast_stun");

        Callable.From(() => { __instance.SetReadyToEndTurn(player, false); }).CallDeferred();

        __result = Task.CompletedTask;
        return false;
    }
    
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    [HarmonyPostfix]
    public static void ModifyDamageBasedOnSanity(IRunState runState, CombatState? combatState, Creature? target, Creature? dealer, decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType, CardPreviewMode previewMode, ref decimal __result)
    {
         if (dealer == null) return;
         var player = dealer.Player; 
         if (player == null || !IsLimbusCharacter(player)) return; 
         if (previewMode != CardPreviewMode.None) return;
         if ((modifyDamageHookType & ModifyDamageHookType.Multiplicative) == 0) return;
         var sp = SanityManager.GetSanity(player); 
         if (sp >= 0) return;
         var absSp = Math.Abs(sp);
         var chance = absSp * 3f;
         if (dealer.CombatState == null) return;
         var roll = dealer.CombatState.RunState.Rng.Niche.NextFloat() * 100f;
         if (roll >= chance) return;
         var reductionPct = absSp * 1.5f;
         var multiplier = 1m - ((decimal)reductionPct / 100m);
         __result *= multiplier;
    }

    private static void CheckAndApplyPanic(Player player)
    {
        var sp = SanityManager.GetSanity(player);
        var data = SanityManager.GetData(player);
    
        if (sp <= -30f)
        {
            if (data.PanicPowerType == null) return;

            if (!player.Creature.Powers.Any(p => p.GetType() == data.PanicPowerType))
            {
                var powerInstance = ModelDb.DebugPower(data.PanicPowerType).ToMutable();
                if (powerInstance != null)
                {
                    TaskHelper.RunSafely(PowerCmd.Apply(powerInstance, player.Creature, 1m, player.Creature, null!));
                }
            }
        }
        else if (sp > 0f)
        {
            RemovePanic(player);
        }
    }

    private static void RemovePanic(Player player)
    {
        var panicPower = player.Creature.Powers.FirstOrDefault(p => p is LCPanicPower);
        if (panicPower != null)
        {
            TaskHelper.RunSafely(PowerCmd.Remove(panicPower));
        }
    }
}
