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


            try
            {
                await scope.ServiceProvider.GetRequiredService<IOrchestrationNodeOrchestrator>().Execute(request, stoppingToken).ConfigureAwait(false);
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

    // Why SYSTEM: a background run executes as system by design (FDW-767, confirmed as the
    // permanent model in FDW-825) -- it has no ClaimsPrincipal, and the work it does on a tenant's
    // behalf may read and write whatever that pipeline is configured to touch.
    //
    // Why the run's tenant rides on the system context: it never narrows what the run sees; it
    // decides the tenant the rows the run writes default to, through every tenant column's
    // DEFAULT. Without it a tenant's execution records and outputs would land in the default
    // tenant, readable by every tenant. A request with no tenant is work that belongs to none, and
    // its rows land in the default tenant -- the column default, not a choice made here.
    internal void EstablishSystemAuthenticationContext(IServiceProvider services, OrchestrationNodeExecutionRequest request)
    {
        var accessor = services.GetService<IAuthenticationContextAccessor>();
        if (accessor is null || accessor.Current is not null)
        {
            return;
        }

        accessor.Current = request.TenantId is { } tenantId
            ? new SystemAuthenticationContext(tenantId)
            : new SystemAuthenticationContext();
        OrchestrationNodeOrchestratorLog.ExecutionElevatedToSystemContext(_logger, request.ExecutionId, request.TenantId);
    }
}
