using System.Threading;
using Fdw.Services.Pipelines.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// Whether a host that serves no hub relays its pipeline events (FDW-3773): the
/// <see cref="SettingDefinitions.PipelineEventRelayEnabled"/> setting, read through the effective settings
/// provider each time an event is sent. Off, or no value, publishes nothing; either is said once, not once per event.
/// </summary>
public sealed class PipelineEventRelaySwitch
{
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ILogger _logger;
    private int _announced;

    /// <summary>Initializes a new instance of the <see cref="PipelineEventRelaySwitch"/> class.</summary>
    public PipelineEventRelaySwitch(IEffectiveSettingsProvider settings, ILogger<PipelineEventRelaySwitch>? logger = null)
    {
        _settings = settings;
        _logger = logger ?? NullLogger<PipelineEventRelaySwitch>.Instance;
    }

    /// <summary>Gets whether the relay is switched on.</summary>
    public bool IsOn()
    {
        if (!_settings.TryGetEffectiveValue<bool>(SettingDefinitions.PipelineEventRelayEnabled, out var on))
        {
            Announce(() => PipelineEventRelayLog.RelaySettingMissing(_logger, SettingDefinitions.PipelineEventRelayEnabled));
            return false;
        }

        if (!on)
        {
            Announce(() => PipelineEventRelayLog.RelayOff(_logger, SettingDefinitions.PipelineEventRelayEnabled));
        }

        return on;
    }

    private void Announce(System.Action log)
    {
        if (Interlocked.Exchange(ref _announced, 1) == 0)
        {
            log();
        }
    }
}
