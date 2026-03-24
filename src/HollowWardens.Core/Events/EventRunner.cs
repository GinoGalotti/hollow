namespace HollowWardens.Core.Events;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

/// <summary>
/// Resolves event options by applying their effects via RunEffectEngine.
/// Checks element thresholds for sacrifice-type events.
/// </summary>
public static class EventRunner
{
    /// <summary>
    /// Checks whether an option can be resolved (e.g., sacrifice threshold met).
    /// </summary>
    public static bool CanResolveOption(EventData evt, int optionIndex, int elementCount = 0)
    {
        var option = evt.Options[optionIndex];
        if (option.ElementThreshold.HasValue && elementCount < option.ElementThreshold.Value)
            return false;
        return true;
    }

    /// <summary>
    /// Resolves the chosen option, applying all its effects to RunState.
    /// Returns false if the threshold is not met (sacrifice events).
    /// </summary>
    public static bool ResolveOption(
        RunState run, EventData evt, int optionIndex,
        Random rng, List<Card>? cards = null, int elementCount = 0)
    {
        if (!CanResolveOption(evt, optionIndex, elementCount)) return false;

        var option = evt.Options[optionIndex];
        foreach (var effect in option.Effects)
            RunEffectEngine.Apply(run, effect, rng, cards);

        return true;
    }
}
