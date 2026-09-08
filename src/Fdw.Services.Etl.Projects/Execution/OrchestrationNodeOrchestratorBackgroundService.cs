using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Background service that dequeues orchestration node execution requests and dispatches them to
/// <see cref="IOrchestrationNodeOrchestrator"/> in per-request scoped DI containers, providing
/// correct service lifetimes and graceful shutdown support.
/// </summary>
/// <remarks>
/// Why BackgroundService with Channel: replaces fire-and-forget Task.Run, giving the host
/// control over in-flight work during shutdown. Each node execution gets its own IServiceScope
/// so IExecutionTracker and IDataGateway have correct scoped lifetimes.
/// </remarks>
public sealed class OrchestrationNodeOrchestratorBackgroundService : BackgroundService
{
    private readonly OrchestrationNodeExecutionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrchestrationNodeOrchestratorBackgroundService> _logger;

    /// <summary>Initializes a new instance of <see cref="OrchestrationNodeOrchestratorBackgroundService"/>.</summary>
    public OrchestrationNodeOrchestratorBackgroundService(
        OrchestrationNodeExecutionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<OrchestrationNodeOrchestratorBackgroundService>? logger = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? NullLogger<OrchestrationNodeOrchestratorBackgroundService>.Instance;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OrchestrationNodeOrchestratorLog.OrchestratorStarted(_logger);

        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProcessRequest(request, stoppingToken).ConfigureAwait(false);
        }

        OrchestrationNodeOrchestratorLog.OrchestratorStopping(_logger);
    }

    private async Task ProcessRequest(OrchestrationNodeExecutionRequest request, CancellationToken stoppingToken)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            EstablishSystemAuthenticationContext(scope.ServiceProvider, request);

            var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestrationNodeOrchestrator>();

            try
            {
                await orchestrator.Execute(request, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                OrchestrationNodeOrchestratorLog.OrchestratorCancelledDuringShutdown(
                    _logger, ex, request.RootNodeId.ToString("N"), request.ExecutionId);
            }
            catch (Exception ex)
            {
                OrchestrationNodeOrchestratorLog.OrchestratorException(
                    _logger, ex, request.RootNodeId.ToString("N"), request.ExecutionId);
            }
        }
    }

    // Why SYSTEM elevation and not the execution's own tenant: a background run has no
    // ClaimsPrincipal, and WorkAuthenticationContext -- the type built for this -- defaults its
    // UserId to the literal "system", which is not a parseable Guid, so it resolves to the
    // deny-everywhere principal and its ActiveTenantId is discarded. The run then reads only
    // shared rows and silently sees a partial result. Elevating is a deliberate, temporary
    // decision (FDW-767): a background execution consequently sees EVERY tenant's rows, so RLS
    // is not a backstop for a schedule-to-tenant routing bug in this path. Revisit by giving the
    // run a real Guid principal -- the schedule's owner, or a per-tenant service principal.
    //
    // Why no TenantId guard: system elevation does not consult TenantId, and returning early
    // without it would leave a tenant-less execution on the deny principal -- half the bug.
    internal void EstablishSystemAuthenticationContext(IServiceProvider services, OrchestrationNodeExecutionRequest request)
    {
        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = new SystemAuthenticationContext();
        OrchestrationNodeOrchestratorLog.ExecutionElevatedToSystemContext(_logger, request.ExecutionId, request.TenantId);
    }
}
