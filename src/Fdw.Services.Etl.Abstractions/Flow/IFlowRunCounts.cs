namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What a run has moved so far, for a step that reports on it (a notification that says how many rows were read).
/// </summary>
public interface IFlowRunCounts
{
    /// <summary>Gets how many rows left a source.</summary>
    long Extracted { get; }

    /// <summary>Gets how many rows were sent to an error output.</summary>
    long Rejected { get; }
}
