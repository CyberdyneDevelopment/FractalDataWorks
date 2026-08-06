using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response for pipeline execution.
/// </summary>
public class ExecutePipelineResponse
{
    /// <summary>
    /// Gets or sets whether execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the unique execution ID.
    /// </summary>
    public Guid? ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the number of records extracted.
    /// </summary>
    public int RecordsExtracted { get; set; }

    /// <summary>
    /// Gets or sets the number of records transformed.
    /// </summary>
    public int RecordsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of records loaded.
    /// </summary>
    public int RecordsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of failed records.
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// True when the failure is "pipeline not found" so the base endpoint can emit
    /// HTTP 404 instead of 500. Why: missing config is a client-correctable error,
    /// not a server fault — Newman and consumers expect 404 here.
    /// </summary>
    public bool NotFound { get; set; }
}
