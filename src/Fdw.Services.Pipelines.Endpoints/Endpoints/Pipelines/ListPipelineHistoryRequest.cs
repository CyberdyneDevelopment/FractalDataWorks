using Fdw.Web.Endpoints.Contracts;

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
    /// Gets or sets an optional success filter. False selects the runs whose stored status is Failed,
    /// so a run that is queued, running or cancelled is in neither list; use <see cref="Status"/> to select those.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Gets or sets an optional filter on the stored status of the run, matched exactly without regard to case.
    /// It is not given together with <see cref="Success"/>.
    /// </summary>
    public string? Status { get; set; }
}
