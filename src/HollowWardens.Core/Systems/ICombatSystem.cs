namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Models;
using HollowWardens.Core.Encounter;

public interface ICombatSystem
{
    void ExecuteActivate(ActionCard action, Territory territory, EncounterState state);
    int CalculateNativeDamagePool(Territory territory);
    void ApplyCounterAttack(Territory territory, Dictionary<Invader, int> damageAssignments);
    void AutoAssignCounterAttack(Territory territory);  // lowest HP first
    void ExecuteAdvance(ActionCard action, EncounterState state);
    void ExecuteHeartMarch(EncounterState state);
}
