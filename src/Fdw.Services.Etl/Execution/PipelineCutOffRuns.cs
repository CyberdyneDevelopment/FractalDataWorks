using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Decides what happens to the runs that were running when the ETL host stopped (FDW-3236): a run is started again as a new run, up to the number of
/// times the setting <see cref="SettingDefinitions.MaxPipelineRunRestarts"/> gives, and is otherwise closed as failed with the reason it is not. The
/// setting has no default in code: while it is not set, no cut-off run is started again, and each is closed naming the setting.
/// </summary>
internal sealed class PipelineCutOffRuns
{
    private readonly IPipelineRunRequestStore _store;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    public PipelineCutOffRuns(IPipelineRunRequestStore store, IServiceScopeFactory scopes, ILogger? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Starts again the cut-off runs the limit allows and closes the others.</summary>
    /// <param name="runs">The runs that were running when the host stopped, oldest first.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The requests of the new runs, to queue in this order.</returns>
    public async Task<IReadOnlyList<PipelineExecutionRequest>> StartAgain(IReadOnlyList<InterruptedRun> runs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runs);
        if (runs.Count == 0)
        {
            return [];
        }

        var limit = ReadLimit();
        var started = new List<PipelineExecutionRequest>();
        foreach (var run in runs)
        {
            if (limit.IsFailure)
            {
                await GiveUp(run, $"the setting {SettingDefinitions.MaxPipelineRunRestarts} is not set, so no cut-off run is started again", cancellationToken).ConfigureAwait(false);
            }
            else if (run.Restarts >= limit.Value)
            {
                await GiveUp(
                    run, $"it was already started again {run.Restarts} times, the number the setting {SettingDefinitions.MaxPipelineRunRestarts} gives", cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var again = await _store.StartAgain(run, cancellationToken).ConfigureAwait(false);
                if (again.IsSuccess && again.Value is not null)
                {
                    PipelineRunRecoveryLog.RunStartedAgain(_logger, run.Request.ExecutionId, run.Request.PipelineName, again.Value.ExecutionId, run.Restarts + 1, limit.Value);
                    started.Add(again.Value);
                }
            }
        }

        return started;
    }

    // The limit is read for the host under the system context, as the trigger queue's capacity is, since no request is in flight when the host starts.
    private IGenericResult<int> ReadLimit()
    {
        using var scope = _scopes.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>();
        if (accessor.Current is null)
        {
            accessor.Current = new SystemAuthenticationContext();
        }

        return PipelineRunRestartLimit.Read(scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>(), _logger);
    }

    private async Task GiveUp(InterruptedRun run, string reason, CancellationToken cancellationToken)
    {
        PipelineRunRecoveryLog.RunNotStartedAgain(_logger, run.Request.ExecutionId, run.Request.PipelineName, reason);
        _ = await _store.GiveUp(run, $"The server stopped while the run was running; it is not started again: {reason}", cancellationToken).ConfigureAwait(false);
    }
}
