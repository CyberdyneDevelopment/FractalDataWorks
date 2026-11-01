namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>The levels that can state thresholds on waiting runs, as a threshold names its owner and as a mode names the levels it applies to (FDW-3161).</summary>
public static class WaitThresholdOwners
{
    /// <summary>The server states thresholds that apply to every pipeline and schedule that takes them.</summary>
    public const string Server = "Server";

    /// <summary>A pipeline states thresholds of its own.</summary>
    public const string Pipeline = "Pipeline";

    /// <summary>A schedule states thresholds of its own.</summary>
    public const string Schedule = "Schedule";
}
