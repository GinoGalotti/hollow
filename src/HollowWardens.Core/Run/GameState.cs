namespace HollowWardens.Core.Run;

using HollowWardens.Core.Models;

public class GameState
{
    public int Weave { get; set; } = 20;
    public int DreadLevel { get; set; } = 1;
    public int TotalFearGenerated { get; set; }
    public List<Card> RunDeck { get; set; } = new();  // persistent across encounters
    public List<Card> PermanentlyRemoved { get; set; } = new();
    public Dictionary<string, int> CorruptionCarryover { get; set; } = new();  // territory → points
}
