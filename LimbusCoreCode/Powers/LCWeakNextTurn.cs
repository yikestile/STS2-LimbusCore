using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCWeakNextTurn : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            Flash();
            await PowerCmd.Apply<WeakPower>(Owner, base.Amount, Owner, null);
            await PowerCmd.Remove( this);
        }
    }
}