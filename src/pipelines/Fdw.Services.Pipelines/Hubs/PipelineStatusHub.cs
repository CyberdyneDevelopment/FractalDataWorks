using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Microsoft.Extensions.Logging;
using Fdw.SignalR;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// SignalR hub for real-time pipeline execution status updates.
/// </summary>
/// <remarks>
/// <para>
/// Built on <see cref="RealTimeHubBase{TClient}"/>: connect/disconnect logging, the uniform
/// <see cref="RealTimeHubBase{TClient}.Subscribe"/>/<see cref="RealTimeHubBase{TClient}.Unsubscribe"/> contract, and
/// the <see cref="RealTimeHubBase{TClient}.OnJoin"/> hook are inherited. The pipeline-specific subscribe
/// verbs are thin key-builders over the inherited contract.
/// </para>
/// <para>
/// Per-org firehose: on connect the hub joins the caller's org group
/// <c>org:{orgId}:pipeline-updates</c>, read from the authenticated principal's <c>org_id</c> claim, so
/// an "all my pipelines" view receives only its own org's pipeline lifecycle events. The broadcaster
/// targets the same group using the pipeline's owning <c>OrgId</c>
/// (the pipe.Pipeline row's OrgId). There is <b>no</b> global (cross-org) firehose: a
/// connection with no <c>org_id</c> claim joins no firehose (logged; no placeholder org), and a
/// pipeline with no owning org is broadcast to no firehose. Clients can additionally opt in to
/// <c>pipeline:{name}</c> / <c>execution:{id}</c> via <see cref="SubscribeToPipeline"/> /
/// <see cref="SubscribeToExecution"/> (FDW-545).
/// </para>
/// </remarks>
public class PipelineStatusHub : RealTimeHubBase<IPipelineStatusHubClient>
{
    /// <inheritdoc/>
    protected override string HubName => "PipelineStatus";

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineStatusHub"/> class.
    /// </summary>
    /// <param name="logger">The logger for hub lifecycle and subscription events.</param>
    /// <param name="executionTracker">
    /// Says which tenant an execution belongs to. Optional so a host that does not register it still starts the hub; every
    /// <c>execution:</c> join on such a host is refused.
    /// </param>
    public PipelineStatusHub(ILogger<PipelineStatusHub> logger, IExecutionTracker? executionTracker = null)
        : base(logger)
    {
        _executionTracker = executionTracker;
    }

    private readonly IExecutionTracker? _executionTracker;

    private const string ExecutionPrefix = "execution:";
    private const string PipelinePrefix = "pipeline:";

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// An <c>execution:{id}</c> group may be joined only by a connection whose tenant is the tenant of that run
    /// (<see cref="IExecutionTracker.GetItem"/>). A run that is not found, a lookup that fails, a connection with no tenant, and a host with no
    /// tracker are all refusals (FDW-2804).
    /// </para>
    /// <para>
    /// A <c>pipeline:{name}</c> group is refused for everyone: a pipeline's configuration exposes no tenant, so there is no owner to compare to.
    /// Nothing is published into it on the API's hub today. When a pipeline's tenant can be read by name this check is added here.
    /// </para>
    /// </remarks>
    protected override async Task<bool> AuthorizeJoin(string scopeKey, CancellationToken cancellationToken)
    {
        if (!CanJoin(scopeKey))
        {
            return false;
        }

        if (scopeKey.StartsWith(PipelinePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return !scopeKey.StartsWith(ExecutionPrefix, StringComparison.Ordinal)
            || await ExecutionIsCallers(scopeKey[ExecutionPrefix.Length..], cancellationToken).ConfigureAwait(false);
    }

    // The run exists and belongs to the caller's tenant. Every other state, including a host with no tracker, is a refusal.
    private async Task<bool> ExecutionIsCallers(string executionKey, CancellationToken cancellationToken)
    {
        if (_executionTracker is null || CallerTenantId is not { } callerTenant || !Guid.TryParse(executionKey, out var executionId))
        {
            return false;
        }

        var item = await _executionTracker.GetItem(executionId, cancellationToken).ConfigureAwait(false);
        return item.IsSuccess
            && item.Value is { } run
            && string.Equals(run.TenantId.ToString("D"), callerTenant, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Joins the caller's org firehose <c>org:{orgId}:pipeline-updates</c> from the authenticated
    /// <c>org_id</c> claim. When the connection carries no org claim the join is skipped and logged —
    /// never a global firehose (there is none) and never a substituted placeholder org (NO FALLBACKS).
    /// </remarks>
    protected override Task OnJoin()
    {
        var orgId = Context.User?.FindFirst("org_id")?.Value;
        if (string.IsNullOrEmpty(orgId))
        {
            SignalRLog.HubOrgClaimMissing(Logger, Context.ConnectionId, HubName);
            return Task.CompletedTask;
        }

        return JoinScope(HubScopes.Org(orgId, "pipeline-updates"));
    }

    /// <summary>
    /// Subscribes the connection to status updates for a specific pipeline.
    /// </summary>
    /// <param name="pipelineName">The pipeline name to subscribe to.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public Task SubscribeToPipeline(string pipelineName) => Subscribe($"pipeline:{pipelineName}");

    /// <summary>
    /// Unsubscribes the connection from status updates for a specific pipeline.
    /// </summary>
    /// <param name="pipelineName">The pipeline name to unsubscribe from.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task UnsubscribeFromPipeline(string pipelineName) => Unsubscribe($"pipeline:{pipelineName}");

    /// <summary>
    /// Subscribes the connection to status updates for a specific execution.
    /// </summary>
    /// <param name="executionId">The execution ID to subscribe to.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public Task SubscribeToExecution(Guid executionId) => Subscribe($"execution:{executionId}");

    /// <summary>
    /// Unsubscribes the connection from status updates for a specific execution.
    /// </summary>
    /// <param name="executionId">The execution ID to unsubscribe from.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task UnsubscribeFromExecution(Guid executionId) => Unsubscribe($"execution:{executionId}");
}
