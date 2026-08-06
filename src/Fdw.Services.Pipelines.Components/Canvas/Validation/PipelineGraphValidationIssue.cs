using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Components;

namespace Fdw.Services.Pipelines.Components.Canvas.Validation;

/// <summary>
/// A single validation issue found during pipeline graph validation.
/// </summary>
/// <param name="Severity">
/// The severity of the issue — a member of the framework <see cref="ValidationSeverities"/>
/// TypeCollection (e.g. <c>ValidationSeverities.Error</c>, <c>ValidationSeverities.Warning</c>).
/// </param>
/// <param name="Message">A human-readable description of the issue.</param>
/// <param name="NodeOrEdgeId">
/// The optional identifier of the node or edge this issue applies to.
/// <c>null</c> when the issue relates to the graph as a whole rather than a specific element.
/// </param>
[ExcludeFromCodeCoverage]
public sealed record PipelineGraphValidationIssue(
    IValidationSeverity Severity,
    string Message,
    string? NodeOrEdgeId = null);
