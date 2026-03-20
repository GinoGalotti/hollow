namespace HollowWardens.Core.Models;

using HollowWardens.Core.Effects;

public class Card
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WardenId { get; set; } = string.Empty;
    public Element[] Elements { get; set; } = Array.Empty<Element>();
    public CardRarity Rarity { get; set; }
    public bool IsStarting { get; set; }
    public bool IsDormant { get; set; }
    public EffectData TopEffect { get; set; } = new();
    public EffectData BottomEffect { get; set; } = new();
    public EffectData? BottomSecondary { get; set; }  // nullable, for compound bottoms
}
