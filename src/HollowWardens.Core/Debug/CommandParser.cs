namespace HollowWardens.Core.Debug;

/// <summary>
/// Result of parsing a dev-console command string.
/// IsValid is false only when the input is empty or lacks a leading slash.
/// Unknown command names are considered valid — the dispatcher decides if it knows them.
/// </summary>
public record ParsedCommand(string Name, string[] Args, bool IsValid, string? Error);

/// <summary>
/// Pure C# command parser for the developer console. No Godot dependency.
/// Parses "/command arg1 arg2" (quoted args supported: /cmd "hello world").
/// </summary>
public static class CommandParser
{
    /// <summary>Parse a raw console input string into a ParsedCommand.</summary>
    public static ParsedCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ParsedCommand("", Array.Empty<string>(), false, "Empty input");

        input = input.Trim();

        if (!input.StartsWith('/'))
            return new ParsedCommand("", Array.Empty<string>(), false, "Commands must start with '/'");

        var tokens = Tokenize(input.Substring(1)); // strip leading slash
        if (tokens.Count == 0)
            return new ParsedCommand("", Array.Empty<string>(), false, "Empty command name");

        var name = tokens[0].ToLowerInvariant();
        var args = tokens.Skip(1).ToArray();
        return new ParsedCommand(name, args, true, null);
    }

    /// <summary>Tokenizes the input, respecting double-quoted strings as single tokens.</summary>
    private static List<string> Tokenize(string input)
    {
        var tokens  = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuote = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '"')
            {
                inQuote = !inQuote;
            }
            else if (c == ' ' && !inQuote)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    /// <summary>Help text for all recognized commands.</summary>
    public static readonly Dictionary<string, string> HelpText = new()
    {
        ["help"]             = "/help [cmd] — List all commands or help for a specific one",
        ["encounter"]        = "/encounter <id> — Start a specific encounter",
        ["restart"]          = "/restart — Restart the current encounter with the same seed",
        ["set_weave"]        = "/set_weave <n> — Set current weave to n",
        ["set_max_weave"]    = "/set_max_weave <n> — Set max weave to n",
        ["set_corruption"]   = "/set_corruption <territory> <pts> — Set corruption on territory",
        ["add_presence"]     = "/add_presence <territory> [n] — Place n presence tokens (default 1)",
        ["set_element"]      = "/set_element <element> <count> — Set element pool",
        ["set_dread"]        = "/set_dread <level> — Set dread level",
        ["spawn"]            = "/spawn <type> <territory> — Spawn an invader unit",
        ["kill_all"]         = "/kill_all — Remove all invaders from the board",
        ["add_card"]         = "/add_card <card_id> — Add a card to hand",
        ["upgrade_card"]     = "/upgrade_card <card_id> <upgrade_id> — Apply card upgrade",
        ["unlock_passive"]   = "/unlock_passive <id> — Force-unlock a passive",
        ["upgrade_passive"]  = "/upgrade_passive <id> — Apply passive upgrade",
        ["give_tokens"]      = "/give_tokens <n> — Add n upgrade tokens",
        ["trigger_event"]    = "/trigger_event <event_id> — Trigger a named event",
        ["export"]           = "/export — Print encounter state as a summary",
        ["run_info"]         = "/run_info — Print current run state",
        ["skip_tide"]        = "/skip_tide — Auto-resolve the current tide",
        ["end_encounter"]    = "/end_encounter [result] — Force-end the encounter (clean/weathered/breach)",
        ["set_terrain"]      = "/set_terrain <territory> <TerrainType> — Set terrain on a territory (Plains/Forest/Mountain/Wetland/Sacred/Scorched/Blighted/Ruins/Fertile)",
    };
}
