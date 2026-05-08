using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public class SanityData
{
    private readonly Player _player;

    public SanityData(Player player)
    {
        _player = player;
    }

    public bool NeedsSanityResetAfterStun
    {
        get => SanityFields.SavedStunReset[_player.Character];
        set => SanityFields.SavedStunReset[_player.Character] = value;
    }

    public Type? PanicPowerType { get; set; }

    public float CurrentSP
    {
        get => (float)SanityFields.SavedSP[_player.Character];
        set
        {
            int newVal = (int)Math.Clamp(Math.Floor(value), -45, 45);
            int oldVal = SanityFields.SavedSP[_player.Character];

            if (oldVal != newVal)
            {
                SanityFields.SavedSP[_player.Character] = newVal;
                OnSanityChanged?.Invoke((float)newVal);
            }
        }
    }

    public event Action<float>? OnSanityChanged;
}

public static class SanityManager
{
    private static readonly ConditionalWeakTable<Player, SanityData> _sanityTable = new();

    public static SanityData GetData(Player player)
    {
        return _sanityTable.GetValue(player, p => new SanityData(p));
    }

    public static float GetSanity(Player player)
    {
        return GetData(player).CurrentSP;
    }

    public static void SetSanity(Player player, float value)
    {
        GetData(player).CurrentSP = value;
    }

    public static void ModifySanity(Player player, float amount)
    {
        var data = GetData(player);
        float oldVal = data.CurrentSP;
        data.CurrentSP += amount;
    }

    public static void SpendSanity(Player player, float amount)
    {
        ModifySanity(player, -amount);
    }

    public static void ResetSanity(Player player)
    {
        GetData(player).CurrentSP = 0f;
    }
}