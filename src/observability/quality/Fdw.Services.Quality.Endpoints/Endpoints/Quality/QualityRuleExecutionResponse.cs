using System;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>One recorded execution of a quality rule: what it judged, when, and what it found.</summary>
public class QualityRuleExecutionResponse
{
    /// <summary>Gets or sets the identifier of the execution.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the rule.</summary>
    public Guid QualityRuleId { get; set; }

    /// <summary>Gets or sets the data set the rule was evaluated over.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field the rule judges, when it judges one.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the rule type.</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity the rule carried when it ran.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Gets or sets the verdict: Passed, Failed, Skipped, NotSupported or NotRun.</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>Gets or sets when the check ran.</summary>
    public DateTimeOffset ExecutedAt { get; set; }

    /// <summary>Gets or sets the number of rows that met the rule.</summary>
    public int PassCount { get; set; }

    /// <summary>Gets or sets the number of rows that violated the rule.</summary>
    public int FailCount { get; set; }

    /// <summary>Gets or sets the number of rows the rule did not judge.</summary>
    public int SkipCount { get; set; }

    /// <summary>Gets or sets how long the evaluation took, in milliseconds.</summary>
    public long? ExecutionDurationMs { get; set; }

    /// <summary>Gets or sets why the rule could not run, when it could not.</summary>
    public string? ErrorMessage { get; set; }
}
