using MegaCrit.Sts2.Core.Entities.Players;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class LimbusUtils
{
    public static bool IsLimbusCharacter(Player? player)
    {
        if (player == null) return false;
        var modelId = player.Character.Id.Entry;
        return modelId.StartsWith("RienSang", StringComparison.OrdinalIgnoreCase) || 
               modelId.StartsWith("LC", StringComparison.OrdinalIgnoreCase);
    }
}