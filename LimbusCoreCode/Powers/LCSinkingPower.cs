using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Saves;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCSinkingPower : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;
    
    public int amount2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("Potency", 0m)
    };
    
    public int Potency
    {
        get => amount2;
        set
        {
            if (amount2 == value) return;
            amount2 = value;
            UpdateDynamicVars();
            InvokeDisplayAmountChanged();
        }
    }
    
    public int Count => (int)base.Amount;
    public override int DisplayAmount => Count;

    public LCSinkingPower()
    {
    }
    
    public LCSinkingPower(int count) : this(count, 1)
    {
    }

    public LCSinkingPower(int count, int potency)
    {
        SetAmount(count);
        amount2 = potency;
        UpdateDynamicVars();
    }
    
    public void AddPotency(int extraPotency)
    {
        AssertMutable();
        Potency += extraPotency;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["Potency"].BaseValue = amount2;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && props.IsPoweredAttack_() && base.Amount > 0)
        {
            Flash();
            
            int strengthReduction = Potency / 5;

            if (strengthReduction > 0)
            {
                await PowerCmd.Apply<StrengthPower>(Owner, -strengthReduction, Owner, null);
                await PowerCmd.Apply<LCStrengthNextTurn>(Owner, strengthReduction, Owner, null);
            }
            
            if (Owner.HasPower<LCGloomingPower>())
            {
                int damageFromSinking = Potency / 5;
                if (damageFromSinking > 0)
                {
                    await CreatureCmd.Damage(choiceContext, Owner, damageFromSinking, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
                }
            }
            
            if (Potency >= 5)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }
}