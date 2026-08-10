using Fdw.Web.Endpoints.Shared;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for pipeline execution history.
/// </summary>
public class ListPipelineHistoryRequest : PaginatedRequest
{
    /// <summary>
    /// Gets or sets an optional pipeline name filter.
    /// </summary>
    public string? PipelineName { get; set; }

    /// <summary>
    /// Gets or sets an optional success filter.
    /// </summary>
    public bool? Success { get; set; }
}
