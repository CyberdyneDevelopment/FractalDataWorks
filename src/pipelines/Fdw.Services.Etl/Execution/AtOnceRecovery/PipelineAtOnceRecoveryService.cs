using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>
/// The sweep that finds the runs carried out at once whose claim lapsed (FDW-6986): at the host's start, and then every <c>PipelineRunClaimRenewalSeconds</c>, it asks the claim procedure for the lapsed
/// rows (a renewal of no run, which renews nothing and answers every row whose lease has passed) and has <see cref="PipelineAtOnceRecovery"/> judge them with the ones other calls answered.
/// A host that does not run pipelines does not register it.
/// </summary>
internal sealed class PipelineAtOnceRecoveryService : BackgroundService
{
    private readonly PipelineAtOnceRecovery _recovery;
    private readonly IServiceScopeFactory _scopes;
    private readonly PipelineRunClaimSettings _settings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineAtOnceRecoveryService"/> class.</summary>
    /// <param name="recovery">Judges the lapsed claims.</param>
    /// <param name="scopes">Gives each sweep a scope of its own, since the claim service is scoped.</param>
    /// <param name="settings">The four numbers of the claim: the renewal interval is the sweep's.</param>
    /// <param name="logger">The logger.</param>
    public PipelineAtOnceRecoveryService(
        PipelineAtOnceRecovery recovery, IServiceScopeFactory scopes, PipelineRunClaimSettings settings, ILogger<PipelineAtOnceRecoveryService>? logger = null)
    {
        _recovery = recovery;
        _scopes = scopes;
        _settings = settings;
        _logger = logger ?? NullLogger<PipelineAtOnceRecoveryService>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await SweepAndSettle(stoppingToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(_settings.RenewalSeconds), stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping: the lapsed claims not yet judged are asked for again by the next host's first sweep, or were answered to a caller that gave them here.
        }
    }

    // Why nothing that goes wrong ends the loop (FDW-6986): a sweep that failed is named and asked for again at the next interval, and a host's background service that throws would stop the host.
    private async Task SweepAndSettle(CancellationToken cancellationToken)
    {
        try
        {
            await Sweep(cancellationToken).ConfigureAwait(false);
            await _recovery.Settle(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = PipelineAtOnceRecoveryLog.SweepFailed(_logger, ex.Message);
        }
    }

    // A renewal of no run: it renews nothing, deletes the rows whose lease has passed and answers them, and the claim service gives them to the recovery.
    private async Task Sweep(CancellationToken cancellationToken)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var swept = await scope.ServiceProvider.GetRequiredService<IPipelineRunClaimService>().Renew(
                [], scope.ServiceProvider.GetRequiredService<IHostEnvironment>().ApplicationName, scope.ServiceProvider.GetRequiredService<ISlotRole>().SlotName, cancellationToken).ConfigureAwait(false);
            if (swept.IsFailure)
            {
                _ = PipelineAtOnceRecoveryLog.SweepFailed(_logger, swept.CurrentMessage);
            }
        }
    }
}
