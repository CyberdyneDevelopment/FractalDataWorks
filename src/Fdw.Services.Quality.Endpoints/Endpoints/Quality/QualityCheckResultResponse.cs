using System;
using System.Collections.Generic;
namespace Fdw.Services.Quality.Endpoints;

/// <summary>Data transfer object representing the result of a quality check execution.</summary>
public class QualityCheckResultResponse
{
    /// <summary>Gets or sets the identifier of the quality rule that was executed.</summary>
    public Guid RuleId { get; set; }

    /// <summary>Gets or sets the execution status (e.g., Passed, Failed, Skipped, NotRun, NotSupported).</summary>
    public string Status { get; set; } = "NotRun";

    /// <summary>Gets or sets the number of rows that failed the quality check.</summary>
    public int FailureCount { get; set; }

    /// <summary>Gets or sets the total number of rows evaluated.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the date and time the check was executed.</summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>Gets or sets the error message if the check failed or was not supported.</summary>
    public string? ErrorMessage { get; set; }
}