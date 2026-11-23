using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The one mapping of a run's database row to the record the execution and history routes return (FDW-6519).
/// </summary>
/// <remarks>
/// Why one mapping: the three routes (one execution, the history of a pipeline, the history list) each carried a copy of these lines, so a field added to the row could
/// be returned by one and not the others. Each route's overridable <c>MapToExecutionRecord</c> now calls this.
/// </remarks>
public static class PipelineExecutionRecordMapping
{
    /// <summary>Maps a database row to the record a route returns.</summary>
    /// <param name="dbRecord">The row of <c>etl.PipelineExecution</c>.</param>
    /// <returns>The record, with the person, run mode, host and slot the row states.</returns>
    public static PipelineExecutionRecord From(PipelineExecutionDbRecord dbRecord) => new()
    {
        ExecutionId = dbRecord.Id,
        PipelineName = dbRecord.PipelineName,
        StartedAt = dbRecord.StartedAt?.UtcDateTime,
        CompletedAt = dbRecord.CompletedAt?.UtcDateTime,
        Status = dbRecord.Status,
        Success = string.Equals(dbRecord.Status, "Succeeded", StringComparison.OrdinalIgnoreCase),
        ErrorMessage = dbRecord.ErrorMessage,
        RecordsExtracted = dbRecord.RecordsExtracted,
        RecordsTransformed = dbRecord.RecordsTransformed,
        RecordsLoaded = dbRecord.RecordsLoaded,
        RecordsFailed = dbRecord.RecordsFailed,
        TotalDurationMs = dbRecord.DurationMs,
        ActingPersonId = dbRecord.ActingPersonId,
        RunMode = dbRecord.RunMode,
        ExecutingHost = dbRecord.ExecutingHost,
        ExecutingSlot = dbRecord.ExecutingSlot,
    };
}
