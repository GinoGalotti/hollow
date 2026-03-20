namespace HollowWardens.Core.Models;

using HollowWardens.Core.Effects;

public class FearActionData
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DreadLevel { get; set; }  // minimum dread level to appear in pool
    public EffectData Effect { get; set; } = new();
    public bool IsAdversarySpecific { get; set; }
    public string? FactionId { get; set; }  // null for global
}
