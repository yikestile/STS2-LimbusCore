using MegaCrit.Sts2.Core.Entities.Players;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class SanityFields
{
    public static readonly SavedSpireField<CharacterModel, int> SavedSP = 
        new(() => 0, "LIMBUS_SP_PERSISTENT");

    public static readonly SavedSpireField<CharacterModel, bool> SavedStunReset = 
        new(() => false, "LIMBUS_STUN_RESET_PERSISTENT");
}