namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;
using HollowWardens.Core.Turn;

/// <summary>
/// Runs the resolution phase: N turns (2/3/1 by tier) where the player plays
/// remaining cards to clean up the board. No new Tide activations occur.
/// Breach is detected if Weave reaches 0 during resolution.
/// </summary>
public class ResolutionRunner
{
    private readonly EffectResolver _resolver;

    public ResolutionRunner(EffectResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Runs the resolution phase. Returns true if a breach occurred mid-resolution.
    /// </summary>
    public bool RunResolution(EncounterState state, IPlayerStrategy strategy)
    {
        int turns = state.Config.ResolutionTurns;

        for (int i = 0; i < turns; i++)
        {
            GameEvents.ResolutionTurnStarted?.Invoke(i + 1);
            GameEvents.PhaseChanged?.Invoke(TurnPhase.Resolution);

            // Refill hand from remaining draw pile
            state.Deck?.RefillHand();

            // Player plays tops during resolution
            var hand = state.Deck?.Hand;
            if (hand != null)
            {
                Card? card;
                while ((card = strategy.ChooseTopPlay(hand, state)) != null)
                {
                    state.Deck?.PlayTop(card);
                    state.Elements?.AddElements(card.Elements, 1);
                    try
                    {
                        var effect = _resolver.Resolve(card.TopEffect);
                        effect.Resolve(state, new TargetInfo());
                    }
                    catch (NotImplementedException) { }

                    if (state.Weave?.IsGameOver == true)
                        return true;
                }
            }
        }

        return state.Weave?.IsGameOver == true;
    }
}
