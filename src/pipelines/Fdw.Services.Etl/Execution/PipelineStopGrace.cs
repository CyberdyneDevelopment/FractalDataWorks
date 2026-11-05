using System;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// How long a stopping ETL host lets the pipeline runs in flight go on before it stops them (FDW-3786): the setting
/// <see cref="SettingDefinitions.PipelineStopGraceSeconds"/>, read once when the host starts.
/// </summary>
/// <remarks>
/// Why no default: how long a stop waits for a load decides how much of it is lost, and only a person can say what that costs. A host without
/// the setting does not start, naming it. Why the shutdown timeout is twice the time: the host's shutdown timeout bounds the whole stop, which is the
/// time given to the runs and then the time to close the records of those that were stopped; the service unit's stop timeout must be longer still.
/// </remarks>
public sealed class PipelineStopGrace
{
    private PipelineStopGrace(TimeSpan duration)
    {
        Duration = duration;
    }

    /// <summary>Gets how long the runs in flight are given to finish when the host stops.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets how long the host may take to stop altogether: the time given to the runs and as long again to close the records of those stopped.</summary>
    public TimeSpan ShutdownTimeout => Duration + Duration;

    /// <summary>Builds the stated time.</summary>
    /// <param name="duration">The time; positive.</param>
    /// <returns>The time.</returns>
    public static PipelineStopGrace Of(TimeSpan duration)
    {
        return duration > TimeSpan.Zero
            ? new PipelineStopGrace(duration)
            : throw new ArgumentOutOfRangeException(nameof(duration), duration, "The time a stopping host gives its runs is positive.");
    }

    /// <summary>Reads the setting.</summary>
    /// <param name="settings">The effective settings.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The time, or a failure naming the setting when it is not set to a positive number of seconds.</returns>
    public static IGenericResult<PipelineStopGrace> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        var seconds = settings.GetEffectiveValue<int>(SettingDefinitions.PipelineStopGraceSeconds);
        return seconds <= 0
            ? GenericResult<PipelineStopGrace>.Failure(PipelineRunRecoveryLog.StopGraceSettingMissing(logger, SettingDefinitions.PipelineStopGraceSeconds))
            : GenericResult<PipelineStopGrace>.Success(Of(TimeSpan.FromSeconds(seconds)));
    }

    /// <summary>
    /// Builds the time the host stops with: read once for the host under the system context, since no request is in flight when the host starts.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The time.</returns>
    /// <exception cref="InvalidOperationException">The setting is missing or not positive; the host does not start without it.</exception>
    public static PipelineStopGrace Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<PipelineStopGrace>();
        using var scope = services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>();
        if (accessor.Current is null)
        {
            accessor.Current = new SystemAuthenticationContext();
        }

        var grace = Read(scope.ServiceProvider.GetRequiredService<IEffectiveSettingsProvider>(), logger);
        return grace.IsSuccess && grace.Value is not null
            ? grace.Value
            : throw new InvalidOperationException(string.Concat(grace.CurrentMessage));
    }
}
