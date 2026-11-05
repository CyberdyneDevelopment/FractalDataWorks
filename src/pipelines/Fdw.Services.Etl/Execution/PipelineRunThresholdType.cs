using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The threshold a pipeline's runs are judged against (FDW-3864): a rule over how its runs have been ending, such as "consecutiveFailures &gt;= 3 DisablePipeline". The figures are
/// what <see cref="PipelineFailureReporter"/> reports after each real run ends; a failure that is transient (a dependency that could not be reached) is not counted.
/// </summary>
[TypeOption(typeof(ThresholdTypes), "PipelineRun")]
public sealed class PipelineRunThresholdType : ThresholdTypeBase
{
    /// <summary>The subject kind of a pipeline.</summary>
    public const string Subject = "Pipeline";

    /// <summary>The figure: how many runs in a row failed in a way that counts.</summary>
    public const string ConsecutiveFailures = "consecutiveFailures";

    /// <summary>The figure: the category of the newest counted failure, null when it carried none.</summary>
    public const string LastFailureCategory = "lastFailureCategory";

    /// <summary>The detail: the failures that counted, newest first, as a list a consequence reads; not a figure to judge.</summary>
    public const string Failures = "failures";

    /// <summary>Initializes a new instance of the <see cref="PipelineRunThresholdType"/> class.</summary>
    public PipelineRunThresholdType()
        : base("PipelineRun", "How a pipeline's runs have been ending: the failures in a row that count.")
    {
    }

    /// <inheritdoc />
    public override string SubjectKind => Subject;

    /// <inheritdoc />
    public override IReadOnlyList<ThresholdField> Fields { get; } =
    [
        new ThresholdField(ConsecutiveFailures, typeof(int), "runs", "How many runs in a row failed for a reason other than an unreachable dependency; a success ends the count."),
        new ThresholdField(LastFailureCategory, typeof(string), string.Empty, "The category of the newest counted failure; absent when it carried none."),
        new ThresholdField(Failures, typeof(string), string.Empty, "The failures that counted, newest first, for a consequence to name."),
    ];
}
