using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Godot; // Added for logging

namespace LimbusCore.LimbusCoreCode.Powers;


public sealed class LCStaggerPower : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("StaggerLevel", 0m)
    };

    public int StaggerLevel
    {
        get => (int)base.Amount;
        set
        {
            if (base.Amount == value) return;
            SetAmount(value, silent: true);
            UpdateDynamicVars();
            InvokeDisplayAmountChanged();
        }
    }

    public override int DisplayAmount => StaggerLevel;

    public LCStaggerPower() : base()
    {
    }

    public static async Task Apply(PlayerChoiceContext context, Creature target, int staggerLevel, Creature? applier, CardModel? source)
    {
        var p = await PowerCmd.Apply<LCStaggerPower>(context, target, (decimal)staggerLevel, applier, source);
        if (p != null)
        {
            p.StaggerLevel = staggerLevel;
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        await Task.CompletedTask;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["StaggerLevel"].BaseValue = StaggerLevel;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner)
        {
            float multiplier = 1.0f;
            switch (StaggerLevel)
            {
                case 1: // Stagger
                    multiplier = 1.20f; // 20% bonus damage
                    break;
                case 2: // Stagger+
                    multiplier = 1.40f; // 40% bonus damage
                    break;
                case 3: // Stagger++
                    multiplier = 1.60f; // 60% bonus damage
                    break;
            }
            return (decimal)multiplier;
        }
        return 1m;
    }

}
