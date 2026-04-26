using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using LimbusCore.LimbusCoreCode.Extensions;

namespace LimbusCore.LimbusCoreCode.Powers;

public sealed class LCCritDmgUp : LimbusCorePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new("CritBonus", 0m)
    };

    public LCCritDmgUp() : base()
    {
    }

    public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        UpdateDynamicVars();
        await Task.CompletedTask;
    }

    private void UpdateDynamicVars()
    {
        if (DynamicVars == null) return;
        DynamicVars["CritBonus"].BaseValue = Amount * 10;
    }
}
