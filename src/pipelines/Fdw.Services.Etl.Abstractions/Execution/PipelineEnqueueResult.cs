namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// What became of a run offered to the pipeline execution queue (FDW-3161): taken, or not taken with the reason the caller closes the run's record with.
/// </summary>
/// <param name="IsAccepted">Whether the run was taken.</param>
/// <param name="Code">The result code to close the run's record with; empty when the run was taken.</param>
/// <param name="Reason">Why the run was not taken; empty when it was taken.</param>
public sealed record PipelineEnqueueResult(bool IsAccepted, string Code, string Reason)
{
    /// <summary>The result code of a run the queue could not hold because it is full.</summary>
    public const string QueueFullCode = "QueueFull";

    /// <summary>The result code of a run a threshold on waiting runs stopped; the caller tells its own caller the server is busy.</summary>
    public const string ServerBusyCode = "ServerBusy";

    /// <summary>Gets the result for a run that was taken.</summary>
    public static PipelineEnqueueResult Accepted { get; } = new(true, string.Empty, string.Empty);

    /// <summary>Gets the result for a run the queue could not hold.</summary>
    /// <param name="reason">Why, naming the number of runs the queue holds.</param>
    /// <returns>The result.</returns>
    public static PipelineEnqueueResult Full(string reason) => new(false, QueueFullCode, reason);

    /// <summary>The result code of a run the queue refused whose stored request could not be removed; the run's record is closed with it so the stored request is stale.</summary>
    public const string RefusalNotRecordedCode = "RefusalNotRecorded";

    /// <summary>Gets the result for a run the queue refused and could not forget (FDW-4277).</summary>
    /// <param name="reason">Why, naming the pipeline and the failure to remove the request.</param>
    /// <returns>The result.</returns>
    public static PipelineEnqueueResult NotForgotten(string reason) => new(false, RefusalNotRecordedCode, reason);

    /// <summary>Gets the result for a run a threshold on waiting runs stopped.</summary>
    /// <param name="reason">Why, naming the threshold.</param>
    /// <returns>The result.</returns>
    public static PipelineEnqueueResult Stopped(string reason) => new(false, ServerBusyCode, reason);

    /// <summary>Gets whether the run was not taken because the server is busy: the queue is full or a threshold stopped new runs.</summary>
    public bool IsBusy => !IsAccepted;
}
