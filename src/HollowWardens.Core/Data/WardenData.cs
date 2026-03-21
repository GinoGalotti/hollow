namespace HollowWardens.Core.Data;

using HollowWardens.Core.Models;

/// <summary>
/// Complete warden definition loaded from data/wardens/{id}.json.
/// </summary>
public class WardenData
{
    public string WardenId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string Flavor { get; set; } = string.Empty;
    public ElementAffinity ElementAffinity { get; set; } = new();
    public int HandLimit { get; set; } = 5;
    public StartingPresence StartingPresence { get; set; } = new();
    public List<PassiveData> Passives { get; set; } = new();
    public string ResolutionStyle { get; set; } = string.Empty;
    public List<Card> Cards { get; set; } = new();
}

public class ElementAffinity
{
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string Tertiary { get; set; } = string.Empty;
}

public class StartingPresence
{
    public string Territory { get; set; } = "I1";
    public int Count { get; set; } = 1;
}

public class PassiveData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Flavor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Mechanic { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}
