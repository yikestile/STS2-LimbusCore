using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Helpers;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCEvadePower : LimbusCorePower
{
    public static readonly string ID = "EvadePower";
    public static event Action<Creature>? OnEvadeTriggered;

    public static readonly SpireField<Creature, bool> HasEvadedThisTurn = new SpireField<Creature, bool>(() => false);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && props.IsPoweredAttack() && !props.HasFlag(ValueProp.Unpowered))
        {
            if (amount <= base.Amount) 
            {
                Flash();
                HasEvadedThisTurn[Owner] = true;
                OnEvadeTriggered?.Invoke(Owner);
                
                TaskHelper.RunSafely(AfterModifyingHpLostBeforeOsty());
                
                return 0m; 
            }
        }
        return amount;
    }
  
    public override async Task AfterSideTurnStart(CombatSide combatSide, CombatState combatState)
    {
        if (combatSide == CombatSide.Player)
        {
            HasEvadedThisTurn[Owner] = false;
            await PowerCmd.Remove(this);
        }
    }
}