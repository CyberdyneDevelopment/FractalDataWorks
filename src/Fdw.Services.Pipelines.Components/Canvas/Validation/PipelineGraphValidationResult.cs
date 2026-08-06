using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;

namespace Fdw.Services.Pipelines.Components.Canvas.Validation;

/// <summary>
/// The result of running <see cref="PipelineGraphValidator"/> over a <see cref="PipelineCanvasModel"/>.
/// </summary>
public sealed class PipelineGraphValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineGraphValidationResult"/> class.
    /// </summary>
    /// <param name="issues">All issues found during validation (may be empty).</param>
    public PipelineGraphValidationResult(IReadOnlyList<PipelineGraphValidationIssue> issues)
    {
        Issues = issues;
    }

    /// <summary>
    /// Gets all issues found during validation.
    /// </summary>
    public IReadOnlyList<PipelineGraphValidationIssue> Issues { get; }

    /// <summary>
    /// Gets a value indicating whether the graph has no Error-level issues and is safe to persist.
    /// </summary>
    public bool IsValid => Issues.All(i => i.Severity != ValidationSeverities.Error);

    /// <summary>
    /// Gets all Error-level issues.
    /// </summary>
    public IEnumerable<PipelineGraphValidationIssue> Errors =>
        Issues.Where(i => i.Severity == ValidationSeverities.Error);

    /// <summary>
    /// Gets all Warning-level issues.
    /// </summary>
    public IEnumerable<PipelineGraphValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == ValidationSeverities.Warning);
}
