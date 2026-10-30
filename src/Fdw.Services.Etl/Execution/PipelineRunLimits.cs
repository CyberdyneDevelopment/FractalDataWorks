using System;
using Fdw.Results;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads how many pipeline runs may be in flight from the platform's settings (FDW-3085).
/// </summary>
/// <remarks>
/// The server limit is read with no tenant: it bounds the host. The tenant limit is read for the run's tenant, so a tenant may
/// override the server's value for itself. A limit that is not set to a positive number refuses the run and names the setting;
/// no limit is assumed.
/// </remarks>
public static class PipelineRunLimits
{
    /// <summary>Reads the two limits for a run of a tenant.</summary>
    /// <param name="settings">The settings the limits are resolved from.</param>
    /// <param name="tenantId">The tenant the run belongs to, or <see langword="null"/> for a run that belongs to none.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The limits, or the refusal naming the first setting that is not a positive number.</returns>
    public static IGenericResult<PipelineRunLimitValues> Read(IEffectiveSettingsProvider settings, Guid? tenantId, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        var server = settings.GetEffectiveValue<int>(SettingDefinitions.MaxConcurrentPipelineRunsPerServer);
        var tenant = settings.GetEffectiveValue<int>(SettingDefinitions.MaxConcurrentPipelineRunsPerTenant, tenantId);
        var missing = server <= 0 ? SettingDefinitions.MaxConcurrentPipelineRunsPerServer
            : tenant <= 0 ? SettingDefinitions.MaxConcurrentPipelineRunsPerTenant
            : null;
        return missing is not null
            ? GenericResult<PipelineRunLimitValues>.Failure(PipelineRunSchedulerLog.LimitSettingMissing(logger, missing))
            : GenericResult<PipelineRunLimitValues>.Success(new PipelineRunLimitValues(server, tenant));
    }
}
