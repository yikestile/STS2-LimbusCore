using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using LimbusCore.LimbusCoreCode.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace LimbusCore.LimbusCoreCode.Powers;

public class LCPierceFragility : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && power.Amount > 10m)
        {
            power.SetAmount(10); 
        }
        await Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && cardSource != null)
        {
            var property = cardSource.GetType().GetProperty("CurrentDamageType");
            if (property != null)
            {
                var damageType = property.GetValue(cardSource);
                if (damageType?.ToString() == "Pierce")
                {
                    return 1m + (Amount * 0.10m);
                }
            }
        }
        return 1m;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove( this);
        }
    }
}