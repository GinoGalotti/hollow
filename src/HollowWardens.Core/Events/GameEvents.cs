namespace HollowWardens.Core.Events;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Static delegate bus for the logic layer. Uses plain delegate fields (not C# events)
/// so that implementation classes can invoke them directly via ?.Invoke().
/// Godot bridge subscribes with += / -= as normal.
/// </summary>
public static class GameEvents
{
    // Turn lifecycle
    public static Action<TurnPhase>? PhaseChanged;
    public static Action? TurnStarted;
    public static Action? TurnEnded;
    public static Action? RestStarted;

    // Deck / cards
    public static Action<Card, TurnPhase>? CardPlayed;        // top or bottom, which phase
    public static Action<Card>? CardDissolved;                 // bottom played (gone for encounter)
    public static Action<Card>? CardDormant;                   // Root: bottom → dormant
    public static Action<Card>? CardRestDissolved;             // lost to rest-dissolve
    public static Action<Card>? CardAwakened;                  // dormant → active
    public static Action<int>? DeckRefilled;                   // cards drawn at Vigil start
    public static Action<int>? DeckShuffled;                   // rest: discard → deck

    // Elements
    public static Action<Element, int>? ElementChanged;        // element pool updated
    public static Action<Element, int>? ThresholdTriggered;    // tier 1/2/3 fired
    public static Action? ThresholdBanked;                     // player banked for later phase
    public static Action? ElementsDecayed;                     // end-of-turn decay

    // Fear / Dread
    public static Action<int>? FearGenerated;                  // amount
    public static Action<int>? FearSpent;                      // amount
    public static Action? FearActionQueued;                    // face-down card added
    public static Action<FearActionData>? FearActionRevealed;  // card flipped
    public static Action<int>? DreadAdvanced;                  // new dread level
    public static Action? DreadUpgradeApplied;                 // queued actions upgraded

    // Tide steps
    public static Action<TideStep>? TideStepStarted;          // Fear/Activate/Advance/Arrive/Escalate/Preview
    public static Action<ActionCard>? ActionCardRevealed;      // which action this Tide
    public static Action<ActionCard>? NextActionPreviewed;     // end-of-Tide preview

    // Combat
    public static Action<Invader, Territory>? InvaderActivated;
    public static Action<Invader, string, string>? InvaderAdvanced;  // from, to
    public static Action<Invader, Territory>? InvaderArrived;
    public static Action<Invader>? InvaderDefeated;
    public static Action<Native, Territory>? NativeDamaged;
    public static Action<Native, Territory>? NativeDefeated;
    public static Action<Territory, int>? CounterAttackReady;  // territory, total damage pool — UI must assign

    // Board state
    public static Action<Territory, int, int>? CorruptionChanged;  // territory, new points, new level
    public static Action<Territory>? HeartDamageDealt;              // invader marched on Heart
    public static Action<int>? WeaveChanged;                       // new weave value

    // D28 Vulnerability: fired when a territory crosses into Desecrated (Level 3).
    // Subscriber should destroy all Presence in that territory.
    public static Action<Territory>? TerritoryDesecrated;

    // D28 Sacrifice: fired after a player sacrifices Presence for emergency cleanse.
    // UI layer can animate the sacrifice + cleanse.
    public static Action<Territory, int>? PresenceSacrificed;  // (territory, presenceRemoved)

    // Threshold resolution
    public static Action<Element, int, string>? ThresholdPending;   // element, tier, description — awaiting player
    public static Action<Element, int>?         ThresholdExpired;   // element, tier — went unresolved at turn end
    public static Action<Element, int, string>? ThresholdResolved;  // element, tier, description — resolved

    // Simulation: fires after each complete Tide (for stats collection)
    public static Action<int>? TideCompleted;  // tide number

    // Encounter lifecycle
    public static Action<EncounterConfig>? EncounterStarted;
    public static Action<EncounterResult>? EncounterEnded;
    public static Action<int>? ResolutionTurnStarted;
    public static Action<SpawnWave>? WaveLocationsRevealed;    // location preview
    public static Action<SpawnWave>? WaveCompositionRevealed;  // unit breakdown

    public static void ClearAll()
    {
        PhaseChanged = null;
        TurnStarted = null;
        TurnEnded = null;
        RestStarted = null;

        CardPlayed = null;
        CardDissolved = null;
        CardDormant = null;
        CardRestDissolved = null;
        CardAwakened = null;
        DeckRefilled = null;
        DeckShuffled = null;

        ElementChanged = null;
        ThresholdTriggered = null;
        ThresholdBanked = null;
        ElementsDecayed = null;

        FearGenerated = null;
        FearSpent = null;
        FearActionQueued = null;
        FearActionRevealed = null;
        DreadAdvanced = null;
        DreadUpgradeApplied = null;

        TideStepStarted = null;
        ActionCardRevealed = null;
        NextActionPreviewed = null;

        InvaderActivated = null;
        InvaderAdvanced = null;
        InvaderArrived = null;
        InvaderDefeated = null;
        NativeDamaged = null;
        NativeDefeated = null;
        CounterAttackReady = null;

        CorruptionChanged = null;
        HeartDamageDealt = null;
        WeaveChanged = null;

        TerritoryDesecrated = null;
        PresenceSacrificed = null;

        ThresholdPending = null;
        ThresholdExpired = null;
        ThresholdResolved = null;

        TideCompleted = null;

        EncounterStarted = null;
        EncounterEnded = null;
        ResolutionTurnStarted = null;
        WaveLocationsRevealed = null;
        WaveCompositionRevealed = null;
    }
}
