using System;
using System.Collections.Generic;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The four numbers of the durable claim (FDW-6629), each a setting with no default in code: how long a claim is leased, how often its holder renews it, how often a waiting run asks again and
/// how long a call waits for the claim lock.
/// </summary>
/// <param name="LeaseSeconds">The lease the claim procedures give a row, in seconds.</param>
/// <param name="RenewalSeconds">How often the holder renews, in seconds; less than the lease.</param>
/// <param name="WaitingPollSeconds">How often a waiting run asks again, in seconds.</param>
/// <param name="LockTimeoutMilliseconds">How long a procedure call waits for the claim lock, in milliseconds.</param>
public sealed record PipelineRunClaimSettings(int LeaseSeconds, int RenewalSeconds, int WaitingPollSeconds, int LockTimeoutMilliseconds)
{
    /// <summary>Reads the four settings.</summary>
    /// <param name="settings">The effective settings.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The settings, or a failure naming the first that is not set or not usable.</returns>
    public static IGenericResult<PipelineRunClaimSettings> Read(IEffectiveSettingsProvider settings, ILogger logger)
    {
        var lease = settings.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLeaseSeconds);
        var renewal = settings.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimRenewalSeconds);
        var poll = settings.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimWaitingPollSeconds);
        var lockTimeout = settings.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds);

        if (lease < 1)
            return Refused(logger, SettingDefinitions.PipelineRunClaimLeaseSeconds, "it is not set to a positive number of seconds");
        if (renewal < 1 || renewal >= lease)
            return Refused(logger, SettingDefinitions.PipelineRunClaimRenewalSeconds, "it is not set to a positive number of seconds less than the lease");
        if (poll < 1)
            return Refused(logger, SettingDefinitions.PipelineRunClaimWaitingPollSeconds, "it is not set to a positive number of seconds");
        if (lockTimeout < 0)
            return Refused(logger, SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds, "it is not set to a number of milliseconds of 0 or more");

        return GenericResult<PipelineRunClaimSettings>.Success(new PipelineRunClaimSettings(lease, renewal, poll, lockTimeout));
    }

    /// <summary>
    /// Names what stops a host that runs pipelines from starting for want of the four settings (FDW-6939): asked when the host initializes, so a setting that is not set is
    /// found at start and named, and not at the first claim.
    /// </summary>
    /// <remarks>
    /// Only a host that runs pipelines is asked, the test the run services use (<c>PipelineTypes.OperationalConnection</c> is named): the API and the scheduler never claim.
    /// </remarks>
    /// <param name="services">The host's services.</param>
    /// <param name="logger">The logger the messages are recorded with.</param>
    /// <returns>The message of each setting that is not usable, or of a missing settings service; none for a host that does not run pipelines.</returns>
    public static IEnumerable<IGenericMessage> Gaps(IServiceProvider services, ILogger logger)
    {
        if (PipelineTypes.OperationalConnection is not { Length: > 0 })
            return [];

        if (services.GetService<IEffectiveSettingsProvider>() is not { } settings)
            return [PipelineRunClaimLog.SettingsNotRegistered(logger)];

        var read = Read(settings, logger);
        if (read.IsSuccess)
            return [];

        return read.Messages;
    }

    private static IGenericResult<PipelineRunClaimSettings> Refused(ILogger logger, string setting, string reason)
        => GenericResult<PipelineRunClaimSettings>.Failure(PipelineRunClaimLog.SettingNotUsable(logger, setting, reason));
}
