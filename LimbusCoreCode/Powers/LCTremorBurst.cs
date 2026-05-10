using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;
using LimbusCore.LimbusCoreCode.Mechanics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LimbusCore.LimbusCoreCode.Powers;


public sealed class LCTremorBurst : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public LCTremorBurst() : base()
    {
    }

    public static async Task Apply(PlayerChoiceContext context, Creature target, Creature? applier, CardModel? source)
    {
        await PowerCmd.Apply<LCTremorBurst>(context, target, 1m, applier, source);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var tremor = Owner.Powers.OfType<ITremorPower>().FirstOrDefault();
        if (tremor != null)
        {
            await tremor.OnBurst(new ThrowingPlayerChoiceContext(), applier ?? Owner);
        }
        await PowerCmd.Remove(this);
    }
}
