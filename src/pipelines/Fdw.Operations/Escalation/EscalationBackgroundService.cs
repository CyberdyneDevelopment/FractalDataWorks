using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
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
    private readonly ISlotRole _role;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="EscalationBackgroundService"/> class.</summary>
    /// <param name="scopes">Makes the scope each pass works in.</param>
    /// <param name="clock">The clock the timer and the passes use.</param>
    /// <param name="role">Says before each pass whether this copy of the app is live; a standby copy advances no alert (FDW-5870).</param>
    /// <param name="logger">The logger.</param>
    public EscalationBackgroundService(IServiceScopeFactory scopes, TimeProvider clock, ISlotRole role, ILogger<EscalationBackgroundService>? logger = null)
    {
        _scopes = scopes;
        _clock = clock;
        _role = role;
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
        // Read on every pass, not once at start: a swap changes a running copy's role. A standby copy answers requests and advances
        // no alert (the live copy does, in the shared store); a role that cannot be read is not live, and the gate has said why.
        var mayRun = _role.BackgroundWorkMayRun();
        if (mayRun.IsFailure)
        {
            SlotRoleLog.PassSkippedRoleUnknown(_logger, nameof(EscalationBackgroundService));
            return;
        }

        if (mayRun.Value is not true)
        {
            SlotRoleLog.PassSkippedStandby(_logger, nameof(EscalationBackgroundService));
            return;
        }

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
