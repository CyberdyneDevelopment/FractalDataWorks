using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Escalation;

/// <summary>
/// The timer that sends the levels of open escalation alerts as they fall due (FDW-3863).
/// </summary>
/// <remarks>
/// The interval is the setting <see cref="SettingDefinitions.EscalationAdvanceIntervalSeconds"/>, read when the host starts. A host that does not state a positive number does not
/// start the timer and says so; no interval is assumed. Each pass runs under the system context, because no request is in flight, in a scope of its own, since the alerts and
/// the notification services are per scope. A pass that fails is logged by what failed and the timer goes on.
/// </remarks>
public sealed class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="EscalationBackgroundService"/> class.</summary>
    /// <param name="scopes">Makes the scope each pass works in.</param>
    /// <param name="clock">The clock the timer and the passes use.</param>
    /// <param name="logger">The logger.</param>
    public EscalationBackgroundService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<EscalationBackgroundService>? logger = null)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? NullLogger<EscalationBackgroundService>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ReadInterval() is not { } interval)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval, _clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await Pass(stoppingToken).ConfigureAwait(false);
        }
    }

    private TimeSpan? ReadInterval()
    {
        using var scope = _scopes.CreateScope();
        using (new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>()))
        {
            if (scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>().TryGetEffectiveValue<int>(SettingDefinitions.EscalationAdvanceIntervalSeconds, out var seconds) && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }
        }

        EscalationAlertLog.AdvanceIntervalMissing(_logger, SettingDefinitions.EscalationAdvanceIntervalSeconds);
        return null;
    }

    private async Task Pass(CancellationToken stoppingToken)
    {
        var scope = _scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            using (new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>()))
            {
                var advanced = await scope.ServiceProvider.GetRequiredService<IEscalationAlerts>().Advance(_clock.GetUtcNow(), stoppingToken).ConfigureAwait(false);
                if (advanced.IsFailure)
                {
                    EscalationAlertLog.AlertsNotRead(_logger, advanced.CurrentMessage);
                }
            }
        }
    }
}
