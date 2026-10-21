using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Detailed DTO for a pipeline: what it is and the run parameters it declares. A pipeline's components are read through the graph
/// endpoints.
/// </summary>
public class PipelineDetailResponse
{
    /// <summary>Gets or sets the pipeline unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the pipeline type (the engine discriminator, "Graph").</summary>
    public required string PipelineType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether a run of the pipeline waits until someone who holds <c>pipelines:approve</c> approves it (FDW-1937).</summary>
    public bool RequireApprovalToRun { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the declared run parameters.</summary>
    public IList<PipelineParameterDeclaration>? Parameters { get; set; }
}
