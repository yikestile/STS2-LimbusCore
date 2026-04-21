using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class CritRegistry
{
    public static readonly SpireField<Creature, bool> WasLastAttackCrit = new SpireField<Creature, bool>(() => false);
}