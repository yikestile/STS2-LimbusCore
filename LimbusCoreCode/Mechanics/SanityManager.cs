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
    private float _currentSP;
    public bool NeedsSanityResetAfterStun { get; set; }
    public Type? PanicPowerType { get; set; }

    public float CurrentSP
    {
        get => _currentSP;
        set
        {
            float newVal = Math.Clamp(value, -45f, 45f);
            if (Math.Abs(_currentSP - newVal) > 0.01f)
            {
                _currentSP = newVal;
                OnSanityChanged?.Invoke(_currentSP);
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
        return _sanityTable.GetValue(player, _ => new SanityData());
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