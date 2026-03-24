namespace HollowWardens.Core.Cards;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;

/// <summary>
/// Generic card upgrade engine. Reads upgrade slot definitions from Card.UpgradeSlots
/// and applies them. No card-specific logic — all defined in JSON.
/// </summary>
public static class CardUpgrader
{
    /// <summary>
    /// Applies the upgrade with the given ID to the card.
    /// Returns false if already applied or upgrade ID not found.
    /// </summary>
    public static bool ApplyUpgrade(Card card, string upgradeId)
    {
        if (card.AppliedUpgradeIds.Contains(upgradeId))
            return false;

        var slot = card.UpgradeSlots.FirstOrDefault(s => s.Id == upgradeId);
        if (slot == null)
            return false;

        ApplySlot(card, slot);
        card.AppliedUpgradeIds.Add(upgradeId);
        return true;
    }

    private static void ApplySlot(Card card, CardUpgradeSlot slot)
    {
        switch (slot.Slot)
        {
            case "top" when slot.Field == "value":
                card.TopEffect = new EffectData
                    { Type = card.TopEffect.Type, Value = slot.To, Range = card.TopEffect.Range };
                break;

            case "top" when slot.Field == "range":
                card.TopEffect = new EffectData
                    { Type = card.TopEffect.Type, Value = card.TopEffect.Value, Range = slot.To };
                break;

            case "bottom" when slot.Field == "value":
                card.BottomEffect = new EffectData
                    { Type = card.BottomEffect.Type, Value = slot.To, Range = card.BottomEffect.Range };
                break;

            case "bottom" when slot.Field == "range":
                card.BottomEffect = new EffectData
                    { Type = card.BottomEffect.Type, Value = card.BottomEffect.Value, Range = slot.To };
                break;

            case "elements" when slot.Action == "add" && slot.Element != null:
                if (Enum.TryParse<Element>(slot.Element, ignoreCase: true, out var el))
                    card.Elements = card.Elements.Append(el).ToArray();
                break;
        }
    }
}
