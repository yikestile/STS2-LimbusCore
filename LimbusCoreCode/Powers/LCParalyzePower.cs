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
        new("ParalyzeChance", 0m)
    };

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        return Task.CompletedTask;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["ParalyzeChance"].BaseValue = Amount * 30;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered) && amount > 1)
        {
            float chance = 0.3f * (int)Amount;
            if (Owner.CombatState != null)
            {
                float roll = Owner.CombatState.RunState.Rng.Niche.NextFloat();
                if (roll < chance)
                {
                    Flash();
                    _shouldRemove = true;
                    return -(amount - 1m);
                }
            }
        }
        return 0m;
    }

    private bool _shouldRemove = false;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? creature, DamageResult result, ValueProp valueProp, Creature target1, CardModel? cardModel)
    {
        if (creature == Owner && _shouldRemove)
        {
            await PowerCmd.Remove(this);
            _shouldRemove = false;
        }
    }
}