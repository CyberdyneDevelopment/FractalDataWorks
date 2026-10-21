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

    /// <summary>Gets when the run started (UTC).</summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>Gets the moment the run point fired (UTC): the end of the run as far as the run point can know it.</summary>
    DateTimeOffset EndedAt { get; }

    /// <summary>Gets how the run stands at the run point: <c>Succeeded</c> before the run's end, <c>Failed</c> at OnFailure.</summary>
    string Outcome { get; }
}
