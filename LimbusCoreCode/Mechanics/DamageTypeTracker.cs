using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class DamageTypeTracker
{
    public static readonly SpireField<Creature, int> SlashHitCount = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> BluntHitCount = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PierceHitCount = new SpireField<Creature, int>(() => 0);

    public static readonly SpireField<Creature, int> SlashFragilityReward = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> BluntFragilityReward = new SpireField<Creature, int>(() => 0);
    public static readonly SpireField<Creature, int> PierceFragilityReward = new SpireField<Creature, int>(() => 0);
}