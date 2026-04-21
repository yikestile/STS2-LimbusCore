using BaseLib.Abstracts;
using BaseLib.Extensions;
using LimbusCore.LimbusCoreCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using LimbusCore.LimbusCoreCode.Mechanics;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCPoisePower : LimbusCorePower
{
    private class Data
    {
        public AttackCommand? commandToModify;
        public bool forceCrit;
    }
    
    public int amount2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("Potency", 0m),
        new("CritChance", 0m)
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
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;
    public override int DisplayAmount => Count;
    
    public LCPoisePower()
    {
    }

    public LCPoisePower(int count) : this(count, 1)
    {
    }
    
    public LCPoisePower(int count, int potency)
    {
        SetAmount(count);
        amount2 = potency;
        UpdateDynamicVars();
    }
    
    public void AddPotency(int newPotency)
    {
        AssertMutable();
        Potency += newPotency;
    }

    private void UpdateDynamicVars()
    {
        DynamicVars["Potency"].BaseValue = amount2;
        DynamicVars["CritChance"].BaseValue = amount2 * 5;
    }

    protected override object InitInternalData() => new Data();

    public void ForceCrit() => GetInternalData<Data>().forceCrit = true;

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.ModelSource is not CardModel cardModel || cardModel.Owner.Creature != Owner || cardModel.Type != CardType.Attack)
            return Task.CompletedTask;

        // Reset crit flag for new attack
        CritRegistry.WasLastAttackCrit[Owner] = false;

        Data internalData = GetInternalData<Data>();
        if (internalData.commandToModify != null)
            return Task.CompletedTask;

        bool isCrit = internalData.forceCrit;
        
        if (!isCrit && base.Amount > 0)
        {
            decimal critChance = Potency * 0.05m;
            if (Owner.CombatState != null)
            {
                float roll = Owner.CombatState.RunState.Rng.Niche.NextFloat();
                if ((decimal)roll < critChance)
                {
                    isCrit = true;
                }
            }
        }

        if (isCrit)
        {
            internalData.commandToModify = command;
            CritRegistry.WasLastAttackCrit[Owner] = true;
            Flash();
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        Data internalData = GetInternalData<Data>();
        if (internalData.commandToModify != null && cardSource == internalData.commandToModify.ModelSource)
        {
            return 1.2m;
        }
        return 1m;
    }

    public override async Task AfterAttack(AttackCommand command)
    {
        Data internalData = GetInternalData<Data>();
        if (command == internalData.commandToModify)
        {
            internalData.commandToModify = null;
            internalData.forceCrit = false;
            await PowerCmd.Decrement(this);
        }
    }
}