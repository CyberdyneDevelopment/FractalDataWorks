namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>Whether a run that arrives is not taken because a threshold on waiting runs that stops new runs fires (FDW-3161).</summary>
/// <param name="IsStopped">Whether the run is not taken.</param>
/// <param name="Threshold">The name of the threshold that stops it; empty when the run is taken.</param>
/// <param name="Reason">Why the run is not taken, naming the threshold; empty when the run is taken.</param>
public sealed record PipelineRunStopCheck(bool IsStopped, string Threshold, string Reason)
{
    /// <summary>Gets the answer for a run nothing stops.</summary>
    public static PipelineRunStopCheck Open { get; } = new(false, string.Empty, string.Empty);
}
