using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The type a create from a template makes (FDW-6718): the type the template was saved as, read from the stored row. The request takes no type of its own: one that is
/// stated and differs is refused, and a stored template that states none is a coded failure, never a graph.
/// </summary>
public static class GraphTemplateType
{
    /// <summary>Chooses the type a create from <paramref name="template"/> makes.</summary>
    /// <param name="template">The stored template.</param>
    /// <param name="requested">What the request states, or null when it states none.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The template's type, or the coded refusal.</returns>
    public static IGenericResult<string> Of(GraphPipelineConfiguration template, string? requested, ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        if (string.IsNullOrWhiteSpace(template.Implementation))
            return GenericResult<string>.Failure(GraphImplementationLog.ImplementationNotStated(log, $"The stored template '{template.Name}'"));

        return string.IsNullOrWhiteSpace(requested) || string.Equals(requested, template.Implementation, StringComparison.Ordinal)
            ? GenericResult<string>.Success(template.Implementation)
            : GenericResult<string>.Failure(GraphImplementationLog.ImplementationChanged(log, requested, template.Implementation));
    }
}
