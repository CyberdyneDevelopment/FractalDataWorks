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
        // Why NullLogger fallback: per FDW convention, ensures the service remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<OrchestrationNodeOrchestratorBackgroundService>.Instance;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OrchestrationNodeOrchestratorLog.OrchestratorStarted(_logger);

        // Why ReadAllAsync with ConfigureAwait: respects the stoppingToken — when the host
        // shuts down, the loop exits after the current item completes, providing graceful shutdown.
        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProcessRequest(request, stoppingToken).ConfigureAwait(false);
        }

        OrchestrationNodeOrchestratorLog.OrchestratorStopping(_logger);
    }

    private async Task ProcessRequest(OrchestrationNodeExecutionRequest request, CancellationToken stoppingToken)
    {
        // Why CreateAsyncScope: each node execution needs its own DI scope so scoped services
        // (IExecutionTracker, IDataGateway, IOrchestrationNodeOrchestrator) have correct lifetimes.
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            // Why here, before resolving IOrchestrationNodeOrchestrator: mirrors
            // PipelineExecutionBackgroundService's seam — a background execution has no
            // ClaimsPrincipal, so without this the scoped IAuthenticationContext stays absent and RLS
            // SESSION_CONTEXT is never set for this scope's connections. No-op when the accessor isn't
            // registered, the request carries no TenantId, or the scope already has a context.
            EstablishWorkAuthenticationContext(scope.ServiceProvider, request);

            var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestrationNodeOrchestrator>();

            try
            {
                await orchestrator.Execute(request, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
            {
                // Why: cancellation is expected during graceful shutdown — observe at Debug.
                // The orchestrator is responsible for recording any partial completion.
                OrchestrationNodeOrchestratorLog.OrchestratorCancelledDuringShutdown(
                    _logger, ex, request.RootNodeId.ToString("N"), request.ExecutionId);
            }
            catch (Exception ex)
            {
                // Why catch-all here: prevents the background service loop from dying on
                // an unexpected error in one request. Each request is isolated.
                OrchestrationNodeOrchestratorLog.OrchestratorException(
                    _logger, ex, request.RootNodeId.ToString("N"), request.ExecutionId);
            }
        }
    }

    // Why: stamps the per-execution scope's ambient IAuthenticationContext (via the AsyncLocal-backed
    // IAuthenticationContextAccessor) with a WorkAuthenticationContext carrying request.TenantId, so
    // every MsSqlConnection created for the rest of this scope sets RLS SESSION_CONTEXT('TenantId').
    // Mirrors PipelineExecutionBackgroundService.EstablishWorkAuthenticationContext.
    // Why internal (not private): unit-tested directly by Fdw.Services.Etl.Projects.Tests via
    // InternalsVisibleTo, rather than standing up the full node-orchestration dependency graph.
    internal void EstablishWorkAuthenticationContext(IServiceProvider services, OrchestrationNodeExecutionRequest request)
    {
        if (!request.TenantId.HasValue)
        {
            return;
        }

        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = new WorkAuthenticationContext(request.TenantId.Value);
        OrchestrationNodeOrchestratorLog.WorkAuthenticationContextEstablished(_logger, request.ExecutionId, request.TenantId.Value);
    }
}
