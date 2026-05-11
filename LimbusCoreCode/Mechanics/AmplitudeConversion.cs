using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace LimbusCore.LimbusCoreCode.Mechanics;

public static class AmplitudeConversion
{
    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold1Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold1");
    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold2Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold2");
    private static readonly AccessTools.FieldRef<TremorMain, int> _raisedHpThreshold3Field =
        AccessTools.FieldRefAccess<TremorMain, int>("_raisedHpThreshold3");
    
    private static readonly AccessTools.FieldRef<TremorMain, int> _staggerAppliedTurnField =
        AccessTools.FieldRefAccess<TremorMain, int>("_staggerAppliedTurn");
    private static readonly AccessTools.FieldRef<TremorMain, int> _thresholdsReachedField =
        AccessTools.FieldRefAccess<TremorMain, int>("_thresholdsReached");

    public static async Task ConvertTremor<T>(PlayerChoiceContext context, Creature target, Creature? applier, CardModel? source) where T : TremorMain, new()
    {
        TremorMain? currentTremor = target.Powers.OfType<TremorMain>().FirstOrDefault();

        int oldPotency = 0;
        int oldCount = 0;
        int oldRaised1 = 0; 
        int oldRaised2 = 0; 
        int oldRaised3 = 0; 
        int oldAppliedTurn = -1;
        int oldFloor = 0;

        if (currentTremor != null)
        {
            oldPotency = currentTremor.Potency;
            oldCount = currentTremor.Count;
            oldRaised1 = _raisedHpThreshold1Field(currentTremor);
            oldRaised2 = _raisedHpThreshold2Field(currentTremor);
            oldRaised3 = _raisedHpThreshold3Field(currentTremor);
            
            oldAppliedTurn = _staggerAppliedTurnField(currentTremor);
            oldFloor = _thresholdsReachedField(currentTremor);

            await PowerCmd.Remove(currentTremor);
        }
        
        await PowerCmd.Apply<T>(context, target, 1m, applier, source); 
        var newTremor = target.GetPower<T>();
        
        if (newTremor != null)
        {
            newTremor.Potency = oldPotency;
            newTremor.Count = oldCount;
            
            _raisedHpThreshold1Field(newTremor) = oldRaised1;
            _raisedHpThreshold2Field(newTremor) = oldRaised2;
            _raisedHpThreshold3Field(newTremor) = oldRaised3;
            
            _staggerAppliedTurnField(newTremor) = oldAppliedTurn;
            _thresholdsReachedField(newTremor) = oldFloor;

            await newTremor.CheckAndApplyStagger(context, target);
        }
    }
}