namespace HollowWardens.Core.Encounter;

using HollowWardens.Core.Models;

public class ActionDeck
{
    private readonly List<ActionCard> _painfulDraw;
    private readonly List<ActionCard> _painfulDiscard = new();
    private readonly List<ActionCard> _easyDraw;
    private readonly List<ActionCard> _easyDiscard = new();
    private readonly Random _rng;

    public ActionDeck(
        IEnumerable<ActionCard> painful,
        IEnumerable<ActionCard> easy,
        Random? rng = null,
        bool shuffle = true)
    {
        _rng = rng ?? new Random();
        _painfulDraw = painful.ToList();
        _easyDraw = easy.ToList();
        if (shuffle)
        {
            Shuffle(_painfulDraw);
            Shuffle(_easyDraw);
        }
    }

    public int PainfulCount => _painfulDraw.Count + _painfulDiscard.Count;
    public int EasyCount    => _easyDraw.Count    + _easyDiscard.Count;
    public int PainfulDrawCount => _painfulDraw.Count;
    public int EasyDrawCount    => _easyDraw.Count;

    /// <summary>
    /// Draws one card from the specified pool. Reshuffles the discard back into the
    /// draw pile automatically when the draw pile is exhausted.
    /// </summary>
    public ActionCard Draw(ActionPool pool)
    {
        var (draw, discard) = pool == ActionPool.Painful
            ? (_painfulDraw, _painfulDiscard)
            : (_easyDraw,    _easyDiscard);

        if (draw.Count == 0)
        {
            if (discard.Count == 0)
                throw new InvalidOperationException($"No cards remain in the {pool} pool.");
            draw.AddRange(discard);
            discard.Clear();
            Shuffle(draw);
        }

        var card = draw[^1];
        draw.RemoveAt(draw.Count - 1);
        discard.Add(card);
        return card;
    }

    /// <summary>Adds an escalation card to the appropriate pool and reshuffles that pool.</summary>
    public void AddEscalationCard(ActionCard card)
    {
        var target = card.Pool == ActionPool.Painful ? _painfulDraw : _easyDraw;
        target.Add(card);
        Shuffle(target);
    }

    private void Shuffle(List<ActionCard> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
