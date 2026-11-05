using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The one place the live view of a run becomes the wire shape (FDW-3382, FDW-3778): the queue endpoint's poll and the
/// components publisher's push both read it, so the console sees one shape from either.
/// </summary>
public static class FlowLiveResponses
{
    /// <summary>The components of a run as the wire shape.</summary>
    /// <param name="views">The components the live view lists.</param>
    public static List<PipelineRunComponentResponse> Components(IEnumerable<FlowLiveComponentView> views) =>
        [.. views.Select(component => new PipelineRunComponentResponse
        {
            Name = component.Name,
            Stage = component.Stage,
            StartedAt = component.StartedAt,
            EndedAt = component.EndedAt,
            Succeeded = component.Succeeded,
            Rows = component.Rows,
            RowsIn = component.RowsIn
        })];

    /// <summary>The links of a run as the wire shape.</summary>
    /// <param name="links">What every link has carried so far.</param>
    public static List<PipelineRunLinkResponse> Links(IEnumerable<FlowLinkCount> links) =>
        [.. links.Select(link => new PipelineRunLinkResponse { From = link.Link.FromComponent, To = link.Link.ToComponent, Rows = link.Rows })];
}
