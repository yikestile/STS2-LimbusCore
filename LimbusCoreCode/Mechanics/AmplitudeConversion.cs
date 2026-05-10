using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class AmplitudeConversion
{
    public static async Task ConvertTremor<T>(PlayerChoiceContext context, Creature target, Creature? applier, CardModel? source) where T : TremorMain, new()
    {
        TremorMain? currentTremor = null;
        foreach (var power in target.Powers)
        {
            if (power is TremorMain tmPower)
            {
                currentTremor = tmPower;
                break;
            }
        }

        int oldPotency = 0;
        int oldCount = 0;

        if (currentTremor != null)
        {
            oldPotency = currentTremor.Potency;
            oldCount = currentTremor.Count;

            await PowerCmd.Remove(currentTremor);
        }
        
        await PowerCmd.Apply<T>(context, target, (decimal)oldCount, applier, source);
        var newTremor = target.GetPower<T>();
        if (newTremor != null)
        {
            newTremor.Potency = oldPotency;

        }
    }
}
