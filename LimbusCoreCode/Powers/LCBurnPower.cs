using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using LimbusCore.LimbusCoreCode.Patches;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCBurnPower : LimbusCorePower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;

    public int amount2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("Potency", 0m)
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
    
    public int Count => (int)base.Amount;
    public override int DisplayAmount => Count;

    public LCBurnPower() : base()
    {
    }

    public static async Task Apply(PlayerChoiceContext context, Creature target, int count, int potency, Creature? applier, CardModel? source)
    {
        var p = await PowerCmd.Apply<LCBurnPower>(context, target, (decimal)count, applier, source);
        if (p != null)
        {
            p.Potency += potency;
        }
    }

    public string GetSecondAmount() => Potency.ToString();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        await Task.CompletedTask;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["Potency"].BaseValue = Potency;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side && Owner.IsAlive && Count > 0 && Potency > 0)
        {
            Flash();
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, (decimal)Potency, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            
            await PowerCmd.Decrement(this);
        }
    }
}
