using Godot;

// Global signal relay for events that don't belong to one specific node.
// Connect to EventBus signals for cross-system communication.
public partial class EventBus : Node
{
    public static EventBus? Instance { get; private set; }

    public override void _Ready() => Instance = this;

    // Territory signals
    [Signal] public delegate void CorruptionChangedEventHandler(string territoryId, int newLevel);
    [Signal] public delegate void TerritoryDesecratedEventHandler(string territoryId);
    [Signal] public delegate void PresencePlacedEventHandler(string territoryId, string wardenId);

    // Invader signals
    [Signal] public delegate void InvaderSpawnedEventHandler(string territoryId);
    [Signal] public delegate void InvaderAdvancedEventHandler(string fromTerritoryId, string toTerritoryId);
    [Signal] public delegate void InvaderRavagedEventHandler(string territoryId);
    [Signal] public delegate void InvaderDefeatedEventHandler(string territoryId);

    // Card signals
    [Signal] public delegate void CardPlayedEventHandler(CardData card, int phase);
    [Signal] public delegate void CardDissolvedEventHandler(CardData card, int tier);
    [Signal] public delegate void CardPermanentlyRemovedEventHandler(CardData card);
}
