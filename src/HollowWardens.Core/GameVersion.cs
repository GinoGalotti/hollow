namespace HollowWardens.Core;

public static class GameVersion
{
    public const int    Major   = 0;
    public const int    Minor   = 8;
    public const int    Patch   = 0;
    public const string Version = "0.8.0";

    public static int Build => _build ??= ComputeBuild();
    private static int? _build;

    private static int ComputeBuild()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "rev-list --count HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                WorkingDirectory       = AppDomain.CurrentDomain.BaseDirectory
            };
            var proc   = System.Diagnostics.Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd().Trim();
            proc?.WaitForExit();
            return int.TryParse(output, out var count) ? count : 0;
        }
        catch { return 0; }
    }

    /// <summary>"0.8.0+342"</summary>
    public static string Full => $"{Version}+{Build}";
}
