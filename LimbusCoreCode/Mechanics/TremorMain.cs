using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using LimbusCore.LimbusCoreCode.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using LimbusCore.LimbusCoreCode.Powers; 
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms; 

namespace LimbusCore.LimbusCoreCode.Mechanics;

public abstract class TremorMain : LimbusCorePower, ITremorPower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public int amount2; // Potency

    private const float STAGGER_THRESHOLD_1 = 0.75f; 
    private const float STAGGER_THRESHOLD_2 = 0.50f; 
    private const float STAGGER_THRESHOLD_3 = 0.25f; 

    protected int _staggerAppliedTurn = -1;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("Potency", 0m),
        new("TremorCount", 0m),
        new("ThresholdsReached", 0m) 
    };

    public int Potency
    {
        get => amount2;
        set
        {
            if (amount2 == value) return;
            amount2 = value;
            UpdateDynamicVars();
            InvokeDisplayAmountChanged();
        }
    }

    public int Count
    {
        get => DynamicVars["TremorCount"].IntValue;
        set
        {
            if (DynamicVars["TremorCount"].IntValue == value) return;
            DynamicVars["TremorCount"].BaseValue = value;
            UpdateDynamicVars();
            InvokeDisplayAmountChanged();
        }
    }
    public override int DisplayAmount => Count;

    public int ThresholdsReached 
    {
        get => DynamicVars["ThresholdsReached"].IntValue;
        set => DynamicVars["ThresholdsReached"].BaseValue = value;
    }

    public TremorMain() : base() { }

    public string GetSecondAmount() => Potency.ToString();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (base.Amount != 1m) SetAmount(1, silent: true);
        UpdateDynamicVars();
        await CheckAndApplyStagger(new ThrowingPlayerChoiceContext(), Owner);
    }

    protected void UpdateDynamicVars()
    {
        DynamicVars["Potency"].BaseValue = Potency;
        DynamicVars["TremorCount"].BaseValue = Count;
    }

    public override async Task AfterCurrentHpChanged(Creature creature1, decimal delta)
    {
        if (creature1 == Owner && delta < 0)
        {
            await CheckAndApplyStagger(new ThrowingPlayerChoiceContext(), creature1);
        }
    }

    public async Task CheckAndApplyStagger(PlayerChoiceContext choiceContext, Creature creature)
    {
        if (creature == null || creature.MaxHp <= 0) return;

        var existingStaggerPower = creature.GetPower<LCStaggerPower>();

        if (!creature.HasPower<TremorMain>())
        {
            if (existingStaggerPower != null) await PowerCmd.Remove(existingStaggerPower);
            _staggerAppliedTurn = -1;
            return;
        }

        float currentHpPercent = (float)creature.CurrentHp / creature.MaxHp;
        float potencyAdjustment = Potency / 100f;

        int totalCrossed = 0;
        if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_3 + potencyAdjustment, 0, 1)) totalCrossed = 3;
        else if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_2 + potencyAdjustment, 0, 1)) totalCrossed = 2;
        else if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_1 + potencyAdjustment, 0, 1)) totalCrossed = 1;

        int targetStaggerLevel = totalCrossed - ThresholdsReached;

        if (targetStaggerLevel <= 0) return;

        int currentTurn = Owner.CombatState?.RoundNumber ?? 0;

        if (targetStaggerLevel > 1)
        {
            if (_staggerAppliedTurn == -1 || _staggerAppliedTurn != currentTurn)
            {
                targetStaggerLevel = 1; 
            }
        }

        if (existingStaggerPower == null)
        {
            await LCStaggerPower.Apply(choiceContext, creature, targetStaggerLevel, Owner, null);
            _staggerAppliedTurn = currentTurn;
        }
        else if (existingStaggerPower.StaggerLevel < targetStaggerLevel)
        {
            existingStaggerPower.StaggerLevel = targetStaggerLevel;
            _staggerAppliedTurn = currentTurn;
        }

        RefreshVisuals(creature);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            if (Count > 0) Count--;

            int currentTurn = Owner.CombatState?.RoundNumber ?? 0;
            

            if (_staggerAppliedTurn != -1 && currentTurn >= _staggerAppliedTurn + 1)
            {
                var staggerPower = Owner.GetPower<LCStaggerPower>();
                if (staggerPower != null)
                {
                    await PowerCmd.Remove(staggerPower);
                }
                _staggerAppliedTurn = -1;
            }
        }
    }

    public override async Task AfterSideTurnStart(CombatSide combatSide, ICombatState state)
    {
        if (combatSide == CombatSide.Player && Owner != null)
        {

            int totalCrossed = CalculateTotalCrossed();
            if (totalCrossed > ThresholdsReached)
            {
                ThresholdsReached = totalCrossed;
            }

            var existingStaggerPower = Owner.GetPower<LCStaggerPower>();
            if (existingStaggerPower != null && existingStaggerPower.StaggerLevel > 1)
            {
                existingStaggerPower.StaggerLevel--;
                RefreshVisuals(Owner);
            }
        }
    }

    private int CalculateTotalCrossed()
    {
        float currentHpPercent = (float)Owner.CurrentHp / Owner.MaxHp;
        float potencyAdjustment = Potency / 100f;

        if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_3 + potencyAdjustment, 0, 1)) return 3;
        if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_2 + potencyAdjustment, 0, 1)) return 2;
        if (currentHpPercent <= Mathf.Clamp(STAGGER_THRESHOLD_1 + potencyAdjustment, 0, 1)) return 1;
        return 0;
    }

    private void RefreshVisuals(Creature creature)
    {
        if (NCombatRoom.Instance != null)
        {
            var creatureNode = NCombatRoom.Instance.GetCreatureNode(creature);
            if (creatureNode != null)
            {
                Overlays.StaggerOverlay.UpdateStaggerOverlay(creatureNode);
            }
        }
    }

    public virtual async Task OnBurst(PlayerChoiceContext context, Creature applier)
    {
        await CheckAndApplyStagger(context, Owner);
        if (Count > 0) Count--;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        var staggerPower = Owner.GetPower<LCStaggerPower>();
        if (staggerPower != null) await PowerCmd.Remove(staggerPower);
        _staggerAppliedTurn = -1;
    }

    public override async Task BeforeCombatStartLate()
    {
        _staggerAppliedTurn = -1;
        ThresholdsReached = 0;
        await Task.CompletedTask;
    }
}