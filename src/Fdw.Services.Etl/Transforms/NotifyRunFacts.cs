using System;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// What a Notify message can say about a run at the moment a notification fires. The variables a message may use, and how
/// each reads its value from these facts, are the options of <see cref="NotifyVariables"/>.
/// </summary>
internal sealed class NotifyRunFacts : INotifyRunFacts
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyRunFacts"/> class.
    /// </summary>
    /// <param name="pipelineId">The pipeline's id, which a notification rule's pipeline scope is matched against.</param>
    /// <param name="pipelineName">The pipeline's name.</param>
    /// <param name="executionId">The execution's id.</param>
    /// <param name="rowsExtracted">Records extracted so far.</param>
    /// <param name="rowsRejected">Records rejected so far.</param>
    /// <param name="rowsLoaded">Records loaded so far.</param>
    public NotifyRunFacts(Guid pipelineId, string pipelineName, Guid executionId, int rowsExtracted, int rowsRejected, int rowsLoaded)
    {
        PipelineId = pipelineId;
        PipelineName = pipelineName;
        ExecutionId = executionId;
        RowsExtracted = rowsExtracted;
        RowsRejected = rowsRejected;
        RowsLoaded = rowsLoaded;
    }

    /// <summary>Gets the pipeline's id.</summary>
    public Guid PipelineId { get; }

    /// <inheritdoc/>
    public string PipelineName { get; }

    /// <inheritdoc/>
    public Guid ExecutionId { get; }

    /// <inheritdoc/>
    public int RowsExtracted { get; }

    /// <inheritdoc/>
    public int RowsRejected { get; }

    /// <inheritdoc/>
    public int RowsLoaded { get; }

    /// <summary>Reads the facts of a run from its result as it stands now.</summary>
    /// <param name="pipelineId">The pipeline's id.</param>
    /// <param name="pipelineName">The pipeline's name.</param>
    /// <param name="result">The run's result so far.</param>
    /// <returns>The facts a message can use.</returns>
    public static NotifyRunFacts Of(Guid pipelineId, string pipelineName, EtlPipelineExecutionResult result) =>
        new(pipelineId, pipelineName, result.ExecutionId, result.RecordsExtracted, result.RecordsFailed, result.RecordsLoaded);
}
