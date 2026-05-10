using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public interface ITremorPower
{
    int Potency { get; set; }
    int Count { get; set; }

    Task OnBurst(PlayerChoiceContext context, Creature applier);
    Task CheckAndApplyStagger(PlayerChoiceContext choiceContext, Creature creature);
}
