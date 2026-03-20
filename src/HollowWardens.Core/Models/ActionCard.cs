namespace HollowWardens.Core.Models;

public class ActionCard
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ActionPool Pool { get; set; }
    public int AdvanceModifier { get; set; }  // 0 = hold, 1 = normal, 2 = +1
    public bool IsEscalation { get; set; }
    // Effects are resolved by CombatSystem based on card ID + unit type modifiers
}
