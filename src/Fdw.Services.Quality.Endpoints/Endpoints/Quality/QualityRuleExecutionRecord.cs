using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// POCO for the <c>dq.QualityRuleExecution</c> table (OpsDb): the verdict of one execution of one quality rule (FDW-1363).
/// </summary>
/// <remarks>
/// <para>
/// Property names MUST match the SQL column names in <c>dq.QualityRuleExecution</c> exactly. OpsDb has no row-level
/// security, so every read and write filters by <see cref="TenantId"/>.
/// </para>
/// <para>
/// A row is appended when a check has run and never changed afterwards: the timeline of a rule is its rows in time order.
/// What a row says is a verdict somebody computed, never a result that was assumed.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class QualityRuleExecutionRecord
{
    /// <summary>Gets or sets the unique identifier of the execution.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the tenant the check ran for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the identifier of the quality rule that was executed.</summary>
    public Guid QualityRuleId { get; set; }

    /// <summary>Gets or sets the data set the rule was evaluated over.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field the rule judges, when it judges one.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the rule type that was evaluated.</summary>
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

    /// <summary>Gets or sets the number of rows the rule did not judge, for example because it could not run.</summary>
    public int SkipCount { get; set; }

    /// <summary>Gets or sets how long the evaluation took, in milliseconds.</summary>
    public long? ExecutionDurationMs { get; set; }

    /// <summary>Gets or sets why the rule could not run, when it could not.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets when the row was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
