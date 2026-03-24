namespace HollowWardens.Core.Models;

/// <summary>
/// Defines a single upgrade slot on a card. Data-driven — read from warden JSON.
/// </summary>
public class CardUpgradeSlot
{
    /// <summary>Unique upgrade ID, e.g. "root_001_u1".</summary>
    public string Id { get; set; } = "";

    /// <summary>"top", "bottom", or "elements".</summary>
    public string Slot { get; set; } = "";

    /// <summary>For top/bottom slots: "value" or "range".</summary>
    public string? Field { get; set; }

    /// <summary>For elements slots: "add".</summary>
    public string? Action { get; set; }

    /// <summary>For elements/add: the element name to add (e.g. "Shadow").</summary>
    public string? Element { get; set; }

    /// <summary>Original value (for documentation/validation).</summary>
    public int From { get; set; }

    /// <summary>New value to set.</summary>
    public int To { get; set; }

    /// <summary>Token cost to apply this upgrade.</summary>
    public int Cost { get; set; } = 1;

    /// <summary>Localization key for the upgrade description.</summary>
    public string DescriptionKey { get; set; } = "";
}
