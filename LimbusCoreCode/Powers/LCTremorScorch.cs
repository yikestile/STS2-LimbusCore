
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using LimbusCore.LimbusCoreCode.Mechanics;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCTremorScorch : TremorMain
{
    public LCTremorScorch() : base()
    {
    }

    public override async Task OnBurst(PlayerChoiceContext context, Creature applier)
    {
        await base.OnBurst(context, applier);

        var burnPower = Owner.GetPower<LCBurnPower>();

        if (burnPower != null)
        {
            int totalPotency = Potency + burnPower.Potency;
            int damage = totalPotency / 2;

            if (damage > 0)
            {
                await CreatureCmd.Damage(context, Owner, (decimal)damage, ValueProp.Unblockable | ValueProp.Unpowered, applier, null);
            }

            if (burnPower.Count > 0)
            {
                await PowerCmd.Decrement(burnPower);
            }
        }
    }
}
