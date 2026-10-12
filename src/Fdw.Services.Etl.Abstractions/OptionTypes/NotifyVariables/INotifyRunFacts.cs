using System;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a Notify message can say about a run at the moment its notification fires: the facts the options of
/// <see cref="NotifyVariables"/> read their values from (FDW-1120).
/// </summary>
public interface INotifyRunFacts
{
    /// <summary>Gets the name of the pipeline that is running.</summary>
    string PipelineName { get; }

    /// <summary>Gets the id of this execution of the pipeline.</summary>
    Guid ExecutionId { get; }

    /// <summary>Gets the records read from the source so far.</summary>
    int RowsExtracted { get; }

    /// <summary>Gets the records rejected so far.</summary>
    int RowsRejected { get; }

    /// <summary>Gets the records loaded so far.</summary>
    int RowsLoaded { get; }
}
