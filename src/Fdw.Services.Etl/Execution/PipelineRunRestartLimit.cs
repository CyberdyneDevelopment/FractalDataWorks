using System;
using Fdw.Results;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads how many times the host starts the same pipeline run again after the host stopped while it ran (FDW-3236). The number is the setting
/// <see cref="SettingDefinitions.MaxPipelineRunRestarts"/>; there is no default in code, so a setting that is not set, or is negative, is a failure.
/// </summary>
public static class PipelineRunRestartLimit
{
    /// <summary>Reads the limit.</summary>
    /// <param name="settings">The settings, read for the host.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The number of times, zero or more; a failure naming the setting when it is not set or is negative.</returns>
    public static IGenericResult<int> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        return settings.TryGetEffectiveValue<int>(SettingDefinitions.MaxPipelineRunRestarts, out var limit) && limit >= 0
            ? GenericResult<int>.Success(limit)
            : GenericResult<int>.Failure(PipelineRunRecoveryLog.RestartLimitMissing(logger, SettingDefinitions.MaxPipelineRunRestarts));
    }
}
