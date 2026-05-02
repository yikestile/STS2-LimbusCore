using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCParalyzePower : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("DamageReduction", 0m) 
    };

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        return Task.CompletedTask;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["DamageReduction"].BaseValue = Amount * 30;
    }

    private bool _shouldRemove = false;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered) && amount > 0)
        {
            decimal reductionPercentage = Amount * 0.3m; // 0.3m for 30% per stack
            
            if (reductionPercentage > 1m) reductionPercentage = 1m;

            _shouldRemove = true;

            return 1m - reductionPercentage;
        }
        return 1m;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp valueProp, Creature target, CardModel? cardModel)
    {
        if (dealer == Owner && _shouldRemove)
        {
            await PowerCmd.Remove(this);
            _shouldRemove = false;
        }
    }
}