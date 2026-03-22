namespace HollowWardens.Core.Models;

public enum Element { Root, Mist, Shadow, Ash, Gale, Void }

public enum CardRarity { Dormant, Awakened, Ancient }

public enum UnitType { Marcher, Ironclad, Outrider, Pioneer }

public enum TerritoryRow { Arrival, Middle, Bridge, Inner }

public enum TurnPhase { Vigil, Tide, Dusk, Rest, Resolution }

public enum TideStep { FearActions, Activate, CounterAttack, Advance, Arrive, Escalate, Preview }

/// <summary>Sub-phase states within an interactive Tide sequence (driven by GameBridge).</summary>
public enum TideSubPhase
{
    None,
    FearActions,      // presenting queued fear actions one by one
    Activate,         // running invader activation (auto)
    CounterAttack,    // waiting for player to assign counter-attack damage
    AdvanceArrive,    // running advance + arrive + escalate + preview (auto)
    WaitAfterCombat,  // all auto steps done — player presses Space to enter Dusk
}

public enum ActionPool { Painful, Easy }

public enum EncounterTier { Standard, Elite, Boss }

public enum EncounterResult { Clean, Weathered, Breach }

public enum BottomResult { Dissolved, Dormant, PermanentlyRemoved }

public enum TokenType { Native, Infrastructure, Bramble, DangerousTerrain }
