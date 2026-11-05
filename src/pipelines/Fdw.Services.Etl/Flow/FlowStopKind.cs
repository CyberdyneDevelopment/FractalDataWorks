namespace Fdw.Services.Etl.Flow;

/// <summary>Why a level's flow was stopped, which decides how its records end (FDW-3880).</summary>
internal enum FlowStopKind
{
    /// <summary>Nothing stopped it: a record ends Completed or Failed by how the flow went.</summary>
    None,

    /// <summary>A person cancelled the run, or its time limit passed: a record whose components did not finish ends Cancelled.</summary>
    Cancelled,

    /// <summary>The host is stopping: the flow writes what it did and leaves the record open for the host to close Interrupted.</summary>
    HostStopped,
}
