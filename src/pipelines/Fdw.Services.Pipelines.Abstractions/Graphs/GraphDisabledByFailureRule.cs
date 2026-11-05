using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Why a pipeline is disabled by its failure rule (FDW-3864): the failures that passed its threshold, newest first. Answered with the graph only for a pipeline whose rule is on, that is
/// disabled, and whose newest counted failures reach the threshold; null otherwise.
/// </summary>
public sealed class GraphDisabledByFailureRule
{
    /// <summary>Gets or sets when the pipeline was last changed, which is when it was disabled.</summary>
    public DateTimeOffset DisabledAt { get; set; }

    /// <summary>Gets or sets how many failed runs in a row counted when the rule fired.</summary>
    public int ThresholdRuns { get; set; }

    /// <summary>Gets or sets why the rule fired, in the words of the threshold evaluator.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the failures that counted, newest first.</summary>
    public List<GraphRuleFailure> Failures { get; set; } = [];
}
