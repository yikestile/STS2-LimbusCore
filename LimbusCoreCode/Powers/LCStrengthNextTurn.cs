using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LimbusCore.LimbusCoreCode.Powers;
public sealed class LCStrengthNextTurn : LimbusCorePower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(Owner, base.Amount, Owner, null);
            await PowerCmd.Remove( this);
        }
    }
}