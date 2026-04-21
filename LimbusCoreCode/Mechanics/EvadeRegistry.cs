using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class EvadeRegistry
{
    public static readonly SpireField<Creature, int> EvadeEffectStacks = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PendingPoisePotency = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PendingPoiseCount = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PendingEnergy = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PendingDraw = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PendingSinkingPotencyAll = new SpireField<Creature, int>(() => 0);
}