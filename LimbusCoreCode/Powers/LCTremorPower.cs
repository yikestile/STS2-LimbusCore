using LimbusCore.LimbusCoreCode.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCTremorPower : TremorMain
{
    public LCTremorPower() : base()
    {
    }
    public static async Task Apply(PlayerChoiceContext context, Creature target, int count, int potency, Creature? applier, CardModel? source)
    {
        var p = await PowerCmd.Apply<LCTremorPower>(context, target, 1m, applier, source);
        if (p != null)
        {
            p.Potency = potency;
            p.Count = count;
        }
    }
}
