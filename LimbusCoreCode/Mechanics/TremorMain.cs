using LimbusCore.LimbusCoreCode.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using LimbusCore.LimbusCoreCode.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat; 
using HarmonyLib; 

namespace LimbusCore.LimbusCoreCode.Mechanics;

public abstract class TremorMain : LimbusCorePower, ITremorPower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public int amount2; // Potency
    public const float STAGGER_THRESHOLD_1_PERCENT = 0.70f;
    public const float STAGGER_THRESHOLD_2_PERCENT = 0.30f;
    public const float STAGGER_THRESHOLD_3_PERCENT = 0.05f;

    public int _staggerAppliedTurn = -1;
    public int _raisedHpThreshold1 = 0;
    public int _raisedHpThreshold2 = 0;
    public int _raisedHpThreshold3 = 0;

    public int _thresholdsReached = 0; 

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("Potency", 0m),
        new("TremorCount", 0m)
    };

    public int Potency { get => amount2; set { if (amount2 == value) return; amount2 = value; UpdateDynamicVars(); InvokeDisplayAmountChanged(); } }
    public int Count { get => DynamicVars["TremorCount"].IntValue; set { if (DynamicVars["TremorCount"].IntValue == value) return; DynamicVars["TremorCount"].BaseValue = value; UpdateDynamicVars(); InvokeDisplayAmountChanged(); } }
    public override int DisplayAmount => Count;

    public TremorMain() : base() { }
    public string GetSecondAmount() => Potency.ToString();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (base.Amount != 1m) SetAmount(1, silent: true);
        UpdateDynamicVars();
        _thresholdsReached = CalculateTotalPhysicalCrossed(0, 0, 0); 
        RefreshVisuals(Owner);
    }

    protected void UpdateDynamicVars() { DynamicVars["Potency"].BaseValue = Potency; DynamicVars["TremorCount"].BaseValue = Count; }

    public override async Task AfterCurrentHpChanged(Creature creature1, decimal delta)
    {
        if (creature1 == Owner && delta < 0) await CheckAndApplyStagger(new ThrowingPlayerChoiceContext(), creature1);
    }

    public async Task CheckAndApplyStagger(PlayerChoiceContext choiceContext, Creature creature)
    {
        if (creature == null || creature.MaxHp <= 0 || !creature.HasPower<TremorMain>()) return;
        
        var existingStaggerPower = creature.GetPower<LCStaggerPower>();
        float hp = creature.CurrentHp;
        float max = creature.MaxHp;

        float line1 = (max * STAGGER_THRESHOLD_1_PERCENT) + _raisedHpThreshold1;
        float line2 = (max * STAGGER_THRESHOLD_2_PERCENT) + _raisedHpThreshold2;
        float line3 = (max * STAGGER_THRESHOLD_3_PERCENT) + _raisedHpThreshold3;

        int totalPhysicalCrossed = 0;
        if (hp <= line1) totalPhysicalCrossed++;
        if (hp <= line2) totalPhysicalCrossed++;
        if (hp <= line3) totalPhysicalCrossed++;

        int targetStaggerLevel = totalPhysicalCrossed - _thresholdsReached;

        if (targetStaggerLevel > 0)
        {
            int currentTurn = Owner.CombatState?.RoundNumber ?? 0;

            if (existingStaggerPower == null)
            {
                await LCStaggerPower.Apply(choiceContext, creature, targetStaggerLevel, Owner, null);
                _staggerAppliedTurn = currentTurn;
            }
            else if (targetStaggerLevel > existingStaggerPower.StaggerLevel)
            {
                existingStaggerPower.StaggerLevel = targetStaggerLevel;
                _staggerAppliedTurn = currentTurn;
            }
        }
        RefreshVisuals(creature);
    }

    public override async Task AfterSideTurnStart(CombatSide combatSide, ICombatState state)
    {
        if (combatSide == CombatSide.Player && Owner != null)
        {
            float hp = Owner.CurrentHp;
            float max = Owner.MaxHp;

            int startingLinesCrossed = 0;
            if (hp <= (max * STAGGER_THRESHOLD_1_PERCENT) + _raisedHpThreshold1) startingLinesCrossed++;
            if (hp <= (max * STAGGER_THRESHOLD_2_PERCENT) + _raisedHpThreshold2) startingLinesCrossed++;
            if (hp <= (max * STAGGER_THRESHOLD_3_PERCENT) + _raisedHpThreshold3) startingLinesCrossed++;

            _thresholdsReached = startingLinesCrossed;

            RefreshVisuals(Owner);
        }
    }

    private int CalculateTotalPhysicalCrossed(int r1, int r2, int r3)
    {
        if (Owner == null) return 0;
        float hp = Owner.CurrentHp;
        float max = Owner.MaxHp;
        int count = 0;
        if (hp <= (max * STAGGER_THRESHOLD_1_PERCENT) + _raisedHpThreshold1) count++;
        if (hp <= (max * STAGGER_THRESHOLD_2_PERCENT) + _raisedHpThreshold2) count++;
        if (hp <= (max * STAGGER_THRESHOLD_3_PERCENT) + _raisedHpThreshold3) count++;
        return count;
    }

    public virtual async Task OnBurst(PlayerChoiceContext context, Creature applier)
    {
        float hp = Owner.CurrentHp;
        float max = Owner.MaxHp;

        if (hp > (max * STAGGER_THRESHOLD_1_PERCENT) + _raisedHpThreshold1) _raisedHpThreshold1 += Potency;
        else if (hp > (max * STAGGER_THRESHOLD_2_PERCENT) + _raisedHpThreshold2) _raisedHpThreshold2 += Potency;
        else if (hp > (max * STAGGER_THRESHOLD_3_PERCENT) + _raisedHpThreshold3) _raisedHpThreshold3 += Potency;
        else _raisedHpThreshold1 += Potency;

        await CheckAndApplyStagger(context, Owner);
        if (Count > 0) Count--;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            if (Count > 0) Count--;
            
            int currentTurn = Owner.CombatState?.RoundNumber ?? 0;
            if (_staggerAppliedTurn != -1 && currentTurn > _staggerAppliedTurn)
            {
                var stagger = Owner.GetPower<LCStaggerPower>();
                if (stagger != null) await PowerCmd.Remove(stagger);
                _staggerAppliedTurn = -1;
            }
        }
    }

    private void RefreshVisuals(Creature creature)
    {
        if (NCombatRoom.Instance == null) return;
        var node = NCombatRoom.Instance.GetCreatureNode(creature);
        if (node == null) return;
        Overlays.StaggerOverlay.UpdateStaggerOverlay(node);
        var stateDisplay = AccessTools.Field(typeof(NCreature), "_stateDisplay").GetValue(node);
        var healthBar = AccessTools.Field(stateDisplay.GetType(), "_healthBar").GetValue(stateDisplay);
        healthBar.GetType().GetMethod("RefreshValues").Invoke(healthBar, null);
    }

    public override async Task AfterCombatEnd(CombatRoom room) 
    { 
        _staggerAppliedTurn = -1; 
        _thresholdsReached = 0; 
        _raisedHpThreshold1 = _raisedHpThreshold2 = _raisedHpThreshold3 = 0; 
    }
    
    public override async Task BeforeCombatStartLate() 
    { 
        _staggerAppliedTurn = -1; 
        _thresholdsReached = 0; 
        _raisedHpThreshold1 = _raisedHpThreshold2 = _raisedHpThreshold3 = 0; 
        await Task.CompletedTask; 
    }
}