using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The result of a dry-run check of a pipeline draft. An empty <see cref="Problems"/> list means creating the
/// pipeline with the same body would not be refused for any reason the check can see.
/// </summary>
public sealed class ValidatePipelineResponse
{
    /// <summary>Gets or sets every problem the check found, in the order the checks ran.</summary>
    public IList<PipelineProblem> Problems { get; set; } = new List<PipelineProblem>();
}
