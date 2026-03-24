namespace HollowWardens.Core.Run;

using HollowWardens.Core.Events;

/// <summary>
/// Drives the run through the realm map: tracks current position, available paths,
/// and draws events for event nodes. All routing logic reads from RealmData — no
/// hardcoded encounter names or paths.
/// </summary>
public class RealmRunner
{
    private readonly RunState _run;
    private readonly RealmData _realm;
    private readonly Random _rng;

    public RealmRunner(RunState run, RealmData realm, Random? rng = null)
    {
        _run   = run;
        _realm = realm;
        _rng   = rng ?? new Random();
    }

    /// <summary>
    /// Returns the encounter node for the current stage.
    /// For stages with encounter_options, selects based on the player's current column.
    /// </summary>
    public MapNode GetCurrentNode()
    {
        if (_run.CurrentNodeIndex >= _realm.Stages.Count)
            return new MapNode { Id = "complete", Type = "complete" };

        var stage = _realm.Stages[_run.CurrentNodeIndex];
        var encId = stage.EncounterId;

        if (encId == null && stage.EncounterOptions != null)
        {
            int col = GetCurrentColumn();
            encId = stage.EncounterOptions.FirstOrDefault(o => o.Column == col)?.EncounterId
                    ?? stage.EncounterOptions.First().EncounterId;
        }

        return new MapNode
        {
            Id          = $"{_realm.Id}_s{stage.Stage}_enc",
            Type        = "encounter",
            EncounterId = encId,
            Column      = 0
        };
    }

    /// <summary>
    /// Returns the post-encounter nodes available for the current stage.
    /// If the current stage is optional and requirements are not met, returns empty.
    /// </summary>
    public List<MapNode> GetAvailableNextNodes()
    {
        if (_run.CurrentNodeIndex >= _realm.Stages.Count) return new();

        var stage = _realm.Stages[_run.CurrentNodeIndex];

        if (stage.IsOptional && stage.RequiresTier1OnPrevious && !HasTier1Result())
            return new();

        return stage.PostEncounterNodes.ToList();
    }

    /// <summary>
    /// Records the chosen node as visited and advances to the next stage.
    /// </summary>
    public void AdvanceToNode(string nodeId)
    {
        _run.VisitedNodeIds.Add(nodeId);
        _run.CurrentNodeIndex++;
    }

    /// <summary>
    /// Returns true when there are no more stages to complete (including optional stages
    /// that cannot be entered).
    /// </summary>
    public bool IsRunComplete()
    {
        if (_run.CurrentNodeIndex >= _realm.Stages.Count) return true;

        var stage = _realm.Stages[_run.CurrentNodeIndex];
        if (stage.IsOptional && stage.RequiresTier1OnPrevious && !HasTier1Result())
            return true;

        return false;
    }

    /// <summary>
    /// Draws a random event for the given node, filtered by the node's tags
    /// and the run's warden ID.
    /// </summary>
    public EventData? DrawEventForNode(MapNode node)
    {
        var all      = EventLoader.LoadAll();
        var filtered = EventLoader.Filter(all, node.Tags.Count > 0 ? node.Tags : null, _run.WardenId);

        // Filter by node type — event nodes only draw choice/warden/sacrifice events;
        // corruption nodes only draw corruption events; merchant nodes only draw merchant events.
        filtered = node.Type switch
        {
            "event"      => filtered.Where(e => e.Type != "corruption" && e.Type != "merchant").ToList(),
            "corruption" => filtered.Where(e => e.Type == "corruption").ToList(),
            "merchant"   => filtered.Where(e => e.Type == "merchant").ToList(),
            _            => filtered
        };

        if (filtered.Count == 0) return null;
        return filtered[_rng.Next(filtered.Count)];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool HasTier1Result() =>
        _run.EncounterResults.Count > 0 &&
        _run.EncounterResults.Last().Equals("clean", StringComparison.OrdinalIgnoreCase);

    private int GetCurrentColumn()
    {
        for (int i = _run.VisitedNodeIds.Count - 1; i >= 0; i--)
        {
            var id = _run.VisitedNodeIds[i];
            foreach (var s in _realm.Stages)
            {
                var node = s.PostEncounterNodes.FirstOrDefault(n => n.Id == id);
                if (node != null) return node.Column;
            }
        }
        return 0;
    }
}
