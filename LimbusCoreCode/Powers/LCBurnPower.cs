using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCBurnPower : LimbusCorePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;

    public int amount2; // Potency

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

    public LCBurnPower() { }
    
    public LCBurnPower(int count, int potency)
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

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side && Owner.IsAlive && Count > 0 && Potency > 0)
        {
            Flash();
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Potency, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            
            await PowerCmd.Decrement(this);
        }
    }
}