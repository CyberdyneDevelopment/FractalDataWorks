namespace Fdw.TUI.Management.Services;

/// <summary>
/// The outcome of a suite run.
/// </summary>
/// <param name="Requests">How many requests were sent.</param>
/// <param name="Assertions">How many assertions were evaluated.</param>
/// <param name="Failures">How many assertions failed.</param>
/// <param name="DurationMs">How long the run took.</param>
/// <param name="Folder">The folder that was run, or null for the whole suite.</param>
public sealed record NewmanRun(int Requests, int Assertions, int Failures, long DurationMs, string? Folder)
{
    /// <summary>Gets a value indicating whether every assertion passed.</summary>
    public bool Passed => Failures == 0;
}
