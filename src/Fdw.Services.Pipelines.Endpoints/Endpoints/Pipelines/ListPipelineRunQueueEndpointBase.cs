using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Lists the pipeline runs in the ETL host's queue (FDW-3097): the runs in flight and the runs waiting, in the order they will start, each
/// waiting run with the reason it cannot start now. The list is the scheduler's own, not a reading of the run records.
/// </summary>
/// <remarks>
/// <para>
/// The route requires <c>pipelines:read</c> and has no anonymous branch for Develop builds. A platform administrator (one who may act on every
/// tenant) reads every tenant's runs. Anyone else reads their own tenant's runs only, and a request that carries no tenant is refused. For
/// that caller a run of another tenant that holds one of their runs back is not named, because its id and the table it writes are that
/// tenant's.
/// </para>
/// <para>
/// It is a route of the host that runs pipelines. A host that does not run them has no queue to list.
/// </para>
/// </remarks>
public abstract class ListPipelineRunQueueEndpointBase : EndpointWithoutRequest<PipelineRunQueueResponse>
{
    private readonly PipelineRunScheduler _scheduler;
    private readonly ITenantContext _tenant;

    /// <summary>Initializes a new instance of the <see cref="ListPipelineRunQueueEndpointBase"/> class.</summary>
    protected ListPipelineRunQueueEndpointBase(ILogger logger, PipelineRunScheduler scheduler, ITenantContext tenant)
    {
        EndpointLogger = logger;
        _scheduler = scheduler;
        _tenant = tenant;
    }

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/etl/queue");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "List the pipeline run queue";
            s.Description = "Returns the pipeline runs that are in flight and the ones waiting, in the order they will start: the caller's tenant's, or every tenant's for a platform administrator. A waiting run names why it cannot start now: the server, tenant or pipeline limit, a table a run in flight writes, a run that must run alone, or a run ahead of it.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var reachesAll = Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User);
        var caller = _tenant.TenantId;
        if (!reachesAll && caller is null)
        {
            OnNoTenant();
            await Send.ResultAsync(TypedResults.Json(new { Error = "The run queue is listed for the caller's tenant, and this request carries none." }, statusCode: 403)).ConfigureAwait(false);
            return;
        }

        var snapshot = _scheduler.Snapshot();
        var owners = snapshot.Running.Concat(snapshot.Waiting).ToDictionary(entry => entry.Run.ExecutionId, entry => entry.Run.TenantId);
        var response = new PipelineRunQueueResponse
        {
            Running = [.. snapshot.Running.Where(entry => Reaches(entry, reachesAll, caller)).Select(entry => Item(entry, reachesAll, caller, owners))],
            Waiting = [.. snapshot.Waiting.Where(entry => Reaches(entry, reachesAll, caller)).Select(entry => Item(entry, reachesAll, caller, owners))],
        };
        OnQueueListed(caller, reachesAll, response.Running.Count, response.Waiting.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Called when the request carries no tenant and its caller does not administer the platform. Override for custom logging.</summary>
    protected virtual void OnNoTenant() { }

    /// <summary>Called when the queue is listed. Override for custom logging.</summary>
    protected virtual void OnQueueListed(Guid? tenantId, bool reachesAll, int running, int waiting) { }

    private static bool Reaches(PipelineRunQueueEntry entry, bool reachesAll, Guid? caller) => reachesAll || entry.Run.TenantId == caller;

    private static PipelineRunQueueItemResponse Item(PipelineRunQueueEntry entry, bool reachesAll, Guid? caller, Dictionary<Guid, Guid?> owners)
    {
        var blockedBy = BlockerOf(entry.Waiting, reachesAll, caller, owners);
        return new PipelineRunQueueItemResponse
        {
            ExecutionId = entry.Run.ExecutionId,
            PipelineId = entry.Run.PipelineId,
            PipelineName = entry.Run.PipelineName,
            TenantId = entry.Run.TenantId,
            QueuedAt = entry.QueuedAt,
            StartedAt = entry.StartedAt,
            WaitReason = entry.Waiting?.Reason.Name,
            Limit = entry.Waiting?.Limit,
            BlockedByExecutionId = blockedBy,
            Table = blockedBy is null ? null : entry.Waiting?.Table,
            HeldUntil = entry.Waiting?.HeldUntil
        };
    }

    // The run that holds this one back, named only when the caller reaches it: every run for a platform administrator, the caller's tenant's
    // own otherwise.
    private static Guid? BlockerOf(PipelineRunBlock? block, bool reachesAll, Guid? caller, Dictionary<Guid, Guid?> owners) =>
        block is { BlockedByExecutionId: { } by } && owners.TryGetValue(by, out var owner) && (reachesAll || owner == caller) ? by : null;
}
