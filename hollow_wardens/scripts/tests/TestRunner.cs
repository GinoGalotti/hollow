using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Headless test runner. Run via:
///   godot --headless --path hollow_wardens scenes/tests/TestRunner.tscn
/// Exits with code 0 on all pass, 1 on any failure.
/// </summary>
public partial class TestRunner : Node
{
    private int _pass = 0;
    private int _fail = 0;
    private readonly List<string> _failures = new();

    public override void _Ready()
    {
        GD.Print("=== Hollow Wardens Tests ===");

        RunAll();

        GD.Print($"\nResults: {_pass} passed, {_fail} failed");
        if (_failures.Count > 0)
        {
            GD.PrintErr("FAILURES:");
            foreach (var f in _failures)
                GD.PrintErr($"  FAIL: {f}");
        }

        int exitCode = _fail > 0 ? 1 : 0;
        GD.Print(exitCode == 0 ? "ALL TESTS PASSED" : "TESTS FAILED");
        GetTree().Quit(exitCode);
    }

    private void RunAll()
    {
        // --- Autoloads ---
        Test("GameState autoload exists", () => GameState.Instance != null);
        Test("EventBus autoload exists", () => EventBus.Instance != null);
        if (GameState.Instance == null) { GD.PrintErr("FATAL: GameState.Instance is null — aborting tests"); GetTree().Quit(1); return; }

        // --- GameState initial values ---
        Test("Weave starts at 20", () => GameState.Instance!.Weave == 20);
        Test("Fear starts at 0", () => GameState.Instance.Fear == 0);
        Test("RunTurn starts at 0", () => GameState.Instance.RunTurn == 0);
        Test("CurrentRealm starts at 1", () => GameState.Instance.CurrentRealm == 1);
        Test("IsRunOver() is false at start", () => !GameState.Instance.IsRunOver());

        // --- GameState.ModifyWeave ---
        {
            int before = GameState.Instance.Weave;
            GameState.Instance.ModifyWeave(-5);
            Test("ModifyWeave(-5) reduces Weave by 5", () => GameState.Instance.Weave == before - 5);
            GameState.Instance.ModifyWeave(5); // restore
        }
        {
            int startWeave = GameState.Instance.Weave;
            GameState.Instance.ModifyWeave(-999);
            Test("ModifyWeave clamps to 0, not negative", () => GameState.Instance.Weave == 0);
            Test("IsRunOver() true when Weave == 0", () => GameState.Instance.IsRunOver());
            GameState.Instance.ModifyWeave(startWeave); // restore
        }

        // --- GameState.ModifyFear / threshold ---
        {
            bool thresholdFired = false;
            int firedThreshold = -1;
            GameState.Instance.FearThresholdReached += (t) => { thresholdFired = true; firedThreshold = t; };

            GameState.Instance.ModifyFear(5);
            Test("ModifyFear(5) triggers threshold 5", () => thresholdFired && firedThreshold == 5);
            Test("Fear resets to 0 after threshold 5", () => GameState.Instance.Fear == 0);

            thresholdFired = false;
            GameState.Instance.ModifyFear(3);
            Test("ModifyFear(3) does not trigger threshold (below 5)", () => !thresholdFired);
            Test("Fear accumulates to 3", () => GameState.Instance.Fear == 3);

            GameState.Instance.ModifyFear(-3); // reset fear to 0
        }

        // --- TerritoryState ---
        {
            var t = new TerritoryState { Id = "test_01" };
            Test("TerritoryState starts with Corruption 0", () => t.Corruption == 0);
            Test("TerritoryState IsDefended false with no Presence", () => !t.IsDefended);

            t.Ravage();
            Test("Ravage() increments Corruption to 1", () => t.Corruption == 1);
            t.Ravage(); t.Ravage();
            Test("Ravage() x3 reaches Corruption 3", () => t.Corruption == 3);
            t.Ravage();
            Test("Ravage() clamps Corruption at 3", () => t.Corruption == 3);

            t.PresenceCount = 1;
            Test("TerritoryState IsDefended true with Presence", () => t.IsDefended);
        }

        // --- CardEffect instantiation ---
        {
            var effect = new CardEffect
            {
                Type = CardEffect.EffectType.GenerateFear,
                Value = 3,
                Range = 1,
                Description = "Generate 3 Fear"
            };
            Test("CardEffect can be instantiated", () => effect != null);
            Test("CardEffect.Type set correctly", () => effect.Type == CardEffect.EffectType.GenerateFear);
            Test("CardEffect.Value set correctly", () => effect.Value == 3);
        }

        // --- CardData instantiation ---
        {
            var card = new CardData
            {
                Id = "root_001",
                CardName = "Test Card",
                WardenId = "root",
                Cost = 2
            };
            Test("CardData can be instantiated", () => card != null);
            Test("CardData.IsDormant defaults to false", () => !card.IsDormant);
            Test("CardData.Id set correctly", () => card.Id == "root_001");
        }

        // --- EncounterData instantiation ---
        {
            var enc = new EncounterData
            {
                Id = "realm1_enc1",
                Tier = EncounterData.EncounterTier.Standard,
                TideSteps = 6,
                ResolutionTurns = 3
            };
            Test("EncounterData can be instantiated", () => enc != null);
            Test("EncounterData.Tier set correctly", () => enc.Tier == EncounterData.EncounterTier.Standard);
            Test("EncounterData.IsEclipse defaults to false", () => !enc.IsEclipse);
            Test("EncounterData.SpawnPattern initialized empty", () => enc.SpawnPattern.Count == 0);
        }

        // --- InvaderData instantiation ---
        {
            var invader = new InvaderData
            {
                Id = "pale_march",
                FactionName = "The Pale March",
                MaxHp = 5,
                MoveSpeed = 1
            };
            Test("InvaderData can be instantiated", () => invader != null);
            Test("InvaderData.WeaveDrainPassive defaults to 0", () => invader.WeaveDrainPassive == 0);
        }

        // --- WardenData instantiation ---
        {
            var warden = new WardenData
            {
                Id = "root",
                WardenName = "The Root",
                StartingHandSize = 6,
                MaxHandSize = 10
            };
            Test("WardenData can be instantiated", () => warden != null);
            Test("WardenData.StartingDeck initialized empty", () => warden.StartingDeck.Count == 0);
        }

        // --- Deck / Hand ---
        {
            var deck = new Deck();
            var card1 = new CardData { Id = "c1", CardName = "Card One" };
            var card2 = new CardData { Id = "c2", CardName = "Card Two" };
            deck.Initialize(new[] { card1, card2 });
            Test("Deck initialized with 2 cards", () => deck.Count == 2);
            var drawn = deck.Draw();
            Test("Deck.Draw() returns a card", () => drawn != null);
            Test("Deck count decreases after Draw()", () => deck.Count == 1);

            var hand = new Hand();
            hand.AddCard(drawn!);
            Test("Hand.AddCard() increases count", () => hand.Count == 1);
            Test("Hand.Cards contains added card", () => hand.Cards[0] == drawn);
            hand.RemoveCard(drawn);
            Test("Hand.RemoveCard() decreases count", () => hand.Count == 0);
        }

        // --- GameState.Territories ---
        {
            var t = new TerritoryState { Id = "t1" };
            GameState.Instance.Territories["t1"] = t;
            Test("GameState.Territories can store territory", () => GameState.Instance.GetTerritory("t1") == t);
            GameState.Instance.Territories.Remove("t1");
        }

        // =====================================================================
        // Phase 2 Tests
        // =====================================================================

        // --- Group A: TerritoryGraph ---
        {
            var graph = new TerritoryGraph();
            Test("TerritoryGraph_AllIds_HasNine", () => graph.AllIds.Count == 9);
        }
        {
            var graph = new TerritoryGraph();
            Test("TerritoryGraph_E2_HasThreeNeighbors", () => graph.GetNeighbors("E2").Count == 3);
        }
        {
            var graph = new TerritoryGraph();
            var neighbors = graph.GetNeighbors("SS");
            Test("TerritoryGraph_SS_IsConnectedToM2_S1_S2",
                () => neighbors.Contains("M2") && neighbors.Contains("S1") && neighbors.Contains("S2"));
        }
        {
            var graph = new TerritoryGraph();
            var result = graph.NextStepToward("E1", new[] { "SS" });
            Test("TerritoryGraph_NextStep_E1_towardSS_ReturnsNeighbor",
                () => result == "E2" || result == "M1");
        }
        {
            var graph = new TerritoryGraph();
            Test("TerritoryGraph_NextStep_AlreadyAtTarget_ReturnsNull",
                () => graph.NextStepToward("SS", new[] { "SS" }) == null);
        }

        // --- Group B: TurnManager ---
        {
            var tm = new TurnManager();
            tm.StartTurn();
            Test("TurnManager_StartTurn_SetsPhaseToVigil",
                () => tm.CurrentPhase == TurnManager.TurnPhase.Vigil);
        }
        {
            var tm = new TurnManager();
            tm.StartTurn();
            tm.EndVigil();
            Test("TurnManager_EndVigil_SetsPhaseToTide",
                () => tm.CurrentPhase == TurnManager.TurnPhase.Tide);
        }
        {
            var tm = new TurnManager();
            tm.StartTurn();
            tm.EndVigil();
            tm.EndTide();
            Test("TurnManager_EndTide_SetsPhaseToToDusk",
                () => tm.CurrentPhase == TurnManager.TurnPhase.Dusk);
        }
        {
            var tm = new TurnManager();
            tm.RecordCardPlayed(TurnManager.TurnPhase.Vigil);
            tm.RecordCardPlayed(TurnManager.TurnPhase.Vigil);
            Test("TurnManager_CanPlayCard_FalseAfterVigilLimit",
                () => !tm.CanPlayCard(TurnManager.TurnPhase.Vigil));
        }
        {
            var tm = new TurnManager();
            tm.IsEclipse = true;
            Test("TurnManager_Eclipse_FlipsLimits",
                () => tm.VigilLimit == 1 && tm.DuskLimit == 2);
        }
        {
            var hand = new Hand();
            var warden = new TestWarden(hand);
            var card = new CardData { Id = "rest_card", CardName = "Rest Card" };
            warden.Discard.Add(card);
            GameState.Instance!.CurrentWarden = warden;
            var tm = new TurnManager();
            tm.PlayerRest();
            Test("TurnManager_PlayerRest_ClearsDiscard",
                () => warden.Discard.Count == 0 && hand.Count == 1);
            GameState.Instance.CurrentWarden = null;
        }

        // --- Group C: TideExecutor SpawnPhase ---
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            GameState.Instance.Territories["E1"] = new TerritoryState { Id = "E1" };
            var enc = new EncounterData();
            enc.SpawnPattern.Add(new SpawnEvent { TideStep = 1, TerritoryId = "E1", Count = 2 });
            var executor = new TideExecutor { EncounterData = enc };
            executor.SpawnPhase(1);
            Test("TideExecutor_Spawn_CorrectStep_AddsUnitsToTerritory",
                () => GameState.Instance.Territories["E1"].InvaderUnits.Count == 2);
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            GameState.Instance.Territories["E1"] = new TerritoryState { Id = "E1" };
            var enc = new EncounterData();
            enc.SpawnPattern.Add(new SpawnEvent { TideStep = 1, TerritoryId = "E1", Count = 2 });
            var executor = new TideExecutor { EncounterData = enc };
            executor.SpawnPhase(2); // wrong step
            Test("TideExecutor_Spawn_WrongStep_NoUnitsAdded",
                () => GameState.Instance.Territories["E1"].InvaderUnits.Count == 0);
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            GameState.Instance.Territories["E1"] = new TerritoryState { Id = "E1" };
            var enc = new EncounterData();
            enc.SpawnPattern.Add(new SpawnEvent { TideStep = 1, TerritoryId = "E1", Count = 3 });
            var executor = new TideExecutor { EncounterData = enc };
            executor.SpawnPhase(1);
            Test("TideExecutor_Spawn_UnitsAppearInActiveInvaders",
                () => GameState.Instance.ActiveInvaders.Count == 3);
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }

        // --- Group D: TideExecutor AdvancePhase ---
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var graph = new TerritoryGraph();
            foreach (var id in graph.AllIds)
                GameState.Instance.Territories[id] = new TerritoryState { Id = id, IsSacredSite = id == "SS" };
            var unit = new InvaderUnit { TerritoryId = "E2", Hp = 5 };
            GameState.Instance.Territories["E2"].InvaderUnits.Add(unit);
            GameState.Instance.ActiveInvaders.Add(unit);
            var executor = new TideExecutor { Graph = graph };
            executor.AdvancePhase();
            Test("TideExecutor_Advance_InvaderMovesTowardSacredSite",
                () => unit.TerritoryId == "M2");
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var graph = new TerritoryGraph();
            foreach (var id in graph.AllIds)
                GameState.Instance.Territories[id] = new TerritoryState { Id = id, IsSacredSite = id == "SS" };
            GameState.Instance.Territories["M1"].PresenceCount = 1;
            var unit = new InvaderUnit { TerritoryId = "E1", Hp = 5 };
            GameState.Instance.Territories["E1"].InvaderUnits.Add(unit);
            GameState.Instance.ActiveInvaders.Add(unit);
            var executor = new TideExecutor { Graph = graph };
            executor.AdvancePhase();
            Test("TideExecutor_Advance_InvaderMovesTowardPresence",
                () => unit.TerritoryId == "M1");
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var graph = new TerritoryGraph();
            foreach (var id in graph.AllIds)
                GameState.Instance.Territories[id] = new TerritoryState { Id = id, IsSacredSite = id == "SS" };
            var unit = new InvaderUnit { TerritoryId = "SS", Hp = 5 };
            GameState.Instance.Territories["SS"].InvaderUnits.Add(unit);
            GameState.Instance.ActiveInvaders.Add(unit);
            var executor = new TideExecutor { Graph = graph };
            executor.AdvancePhase();
            Test("TideExecutor_Advance_UnitAlreadyAtTarget_DoesNotMove",
                () => unit.TerritoryId == "SS");
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }

        // --- Group E: TideExecutor RavagePhase ---
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var t = new TerritoryState { Id = "E1", PresenceCount = 0 };
            var unit = new InvaderUnit { TerritoryId = "E1", Hp = 5 };
            t.InvaderUnits.Add(unit);
            GameState.Instance.Territories["E1"] = t;
            var executor = new TideExecutor();
            executor.RavagePhase();
            Test("TideExecutor_Ravage_UndefendedTerritoryCorruptionIncreases",
                () => t.Corruption == 1);
            GameState.Instance.ModifyWeave(1); // restore weave
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var t = new TerritoryState { Id = "M1", PresenceCount = 1 }; // IsDefended = true
            var unit = new InvaderUnit { TerritoryId = "M1", Hp = 5 };
            t.InvaderUnits.Add(unit);
            GameState.Instance.Territories["M1"] = t;
            var executor = new TideExecutor();
            executor.RavagePhase();
            Test("TideExecutor_Ravage_DefendedTerritoryNotRavaged",
                () => t.Corruption == 0);
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }
        {
            GameState.Instance!.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
            var t = new TerritoryState { Id = "E2", PresenceCount = 0 };
            var unit = new InvaderUnit { TerritoryId = "E2", Hp = 5 };
            t.InvaderUnits.Add(unit);
            GameState.Instance.Territories["E2"] = t;
            int weaveBefore = GameState.Instance.Weave;
            var executor = new TideExecutor();
            executor.RavagePhase();
            Test("TideExecutor_Ravage_WeaveDrains",
                () => GameState.Instance.Weave == weaveBefore - 1);
            GameState.Instance.ModifyWeave(1); // restore
            GameState.Instance.Territories.Clear();
            GameState.Instance.ActiveInvaders.Clear();
        }

        // --- Group F: EncounterManager ---
        {
            var enc = new EncounterData { TideSteps = 1, ResolutionTurns = 1 };
            var em = new EncounterManager();
            em.StartEncounter(enc);
            Test("EncounterManager_Start_InitializesNineTerritories",
                () => GameState.Instance!.Territories.Count == 9);
        }
        {
            var enc = new EncounterData { TideSteps = 1, ResolutionTurns = 1 };
            enc.StartingCorruption["E1"] = 2;
            var em = new EncounterManager();
            em.StartEncounter(enc);
            Test("EncounterManager_Start_AppliesStartingCorruption",
                () => GameState.Instance!.Territories["E1"].Corruption == 2);
        }
        {
            var enc = new EncounterData { TideSteps = 2, ResolutionTurns = 1 };
            var em = new EncounterManager();
            em.StartEncounter(enc);
            em.RunTideStep();
            em.RunTideStep();
            Test("EncounterManager_CheckResolution_TrueAfterAllSteps",
                () => em.CheckForResolution());
        }
        {
            GameState.Instance!.ActiveInvaders.Clear();
            var em = new EncounterManager();
            Test("EncounterManager_EvaluateReward_CleanWhenNoInvaders",
                () => em.EvaluateRewardTier() == 0);
        }
    }

    private void Test(string name, Func<bool> assertion)
    {
        try
        {
            if (assertion())
            {
                GD.Print($"  PASS  {name}");
                _pass++;
            }
            else
            {
                GD.PrintErr($"  FAIL  {name}");
                _failures.Add(name);
                _fail++;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"  FAIL  {name} — threw {ex.GetType().Name}: {ex.Message}");
            _failures.Add($"{name} (exception)");
            _fail++;
        }
    }
}

// Helper for TurnManager_PlayerRest test — allows setting Hand from outside Warden
internal partial class TestWarden : Warden
{
    public TestWarden(Hand hand) { Hand = hand; }
}
