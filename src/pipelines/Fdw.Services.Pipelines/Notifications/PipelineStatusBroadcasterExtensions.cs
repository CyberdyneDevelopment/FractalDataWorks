using System;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The one way a run is announced from the pipeline it runs (FDW-5943).
/// </summary>
/// <remarks>
/// The firehose group a run's status goes to is <c>org:{orgId}:pipeline-updates</c>, and the organization is a property of the pipeline's configuration. A caller that
/// states the pipeline name and the organization separately can leave the organization out, as the trigger paths did, and the organization's live view then never hears of
/// the run. Announcing from the configuration takes both from the same place, so neither can be stated without the other. A pipeline that states no organization is broadcast
/// to no firehose, as <see cref="IPipelineStatusBroadcaster.BroadcastStatusChange"/> documents.
/// </remarks>
public static class PipelineStatusBroadcasterExtensions
{
    /// <summary>Broadcasts a status change of a run of <paramref name="pipeline"/>, addressed to the organization that owns the pipeline.</summary>
    /// <param name="broadcaster">The broadcaster.</param>
    /// <param name="pipeline">The pipeline the run is of: its name is the run's pipeline name and its organization addresses the broadcast.</param>
    /// <param name="executionId">The run's execution item.</param>
    /// <param name="status">The new status.</param>
    /// <param name="message">An optional message.</param>
    /// <returns>A task that completes when the broadcast has been sent.</returns>
    public static Task BroadcastRunStatus(
        this IPipelineStatusBroadcaster broadcaster, IPipelineImplementationConfiguration pipeline, Guid executionId, string status, string? message = null) =>
        broadcaster.BroadcastStatusChange(pipeline.Name, executionId, status, message, pipeline.OrgId);
}
