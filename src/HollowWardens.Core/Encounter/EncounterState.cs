namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Cards;
using HollowWardens.Core.Models;
using HollowWardens.Core.Systems;

public class EncounterState
{
    public EncounterConfig Config { get; set; } = new();
    public List<Territory> Territories { get; set; } = new();
    public IDeckManager? Deck { get; set; }
    public IElementSystem? Elements { get; set; }
    public IDreadSystem? Dread { get; set; }
    public IFearActionSystem? FearActions { get; set; }
    public IWeaveSystem? Weave { get; set; }
    public ICombatSystem? Combat { get; set; }
    public IPresenceSystem? Presence { get; set; }
    public ICorruptionSystem? Corruption { get; set; }
    public ActionCard? CurrentActionCard { get; set; }
    public int CurrentTide { get; set; }
    public TurnPhase CurrentPhase { get; set; }

    public Territory? GetTerritory(string id) => Territories.FirstOrDefault(t => t.Id == id);
    public IEnumerable<Territory> TerritoriesWithInvaders() => Territories.Where(t => t.Invaders.Any(i => i.IsAlive));
    public IEnumerable<Territory> TerritoriesWithNatives() => Territories.Where(t => t.Natives.Any(n => n.IsAlive));
}
