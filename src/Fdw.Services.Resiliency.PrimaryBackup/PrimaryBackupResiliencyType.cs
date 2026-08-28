using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Resiliency;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency.PrimaryBackup;

/// <summary>
/// PrimaryBackup resiliency strategy. On primary source failure:
/// 1. Activates the configured backup data set.
/// 2. Re-runs the stage (backup routing is signaled via extended context).
/// 3. Triggers the refresh schedule to re-sync primary when it recovers.
/// </summary>
/// <remarks>
/// This TypeOption requires an <see cref="IScheduleClient"/> to trigger the refresh schedule.
/// The client is provided via the <see cref="IPrimaryBackupResiliencyContext"/> extended context.
/// </remarks>
[TypeOption(typeof(ResiliencyTypes), "PrimaryBackup")]
public sealed class PrimaryBackupResiliencyType : ResiliencyTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PrimaryBackupResiliencyType"/>.</summary>
    public PrimaryBackupResiliencyType()
        : base(
            id: 3,
            name: "PrimaryBackup",
            displayName: "Primary/Backup",
            description: "On primary failure, route to backup data set and schedule refresh.")
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why: Primary failure triggers backup activation. The executor wraps <paramref name="runStage"/>
    /// to switch the SourceDataSetId in the execution context before the second attempt.
    /// The schedule trigger uses <see cref="IScheduleClient"/> obtained from the DI root
    /// via the extended context (cast to <see cref="IPrimaryBackupResiliencyContext"/>).
    /// </remarks>
    public override async Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        if (runStage == null) throw new ArgumentNullException(nameof(runStage));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        if (config is not PrimaryBackupResiliencyConfiguration pbConfig)
        {
            return GenericResult.Failure(
                PrimaryBackupLog.WrongConfigurationType(NullLogger.Instance, config.GetType().Name));
        }

        if (ctx is not IPrimaryBackupResiliencyContext pbCtx)
        {
            return GenericResult.Failure(
                PrimaryBackupLog.WrongContextType(NullLogger.Instance, ctx.ExecutionId, ctx.GetType().Name));
        }

        // Attempt 1: run with primary source.
        var primaryResult = await runStage(cancellationToken).ConfigureAwait(false);
        if (primaryResult.IsSuccess)
            return primaryResult;

        // Primary failed — signal backup activation to the run delegate via context.
        pbCtx.ActivateBackup(pbConfig.BackupDataSetId);
        ResiliencyLog.BackupSourceActivated(pbCtx.Logger, ctx.ExecutionId, pbConfig.BackupDataSetId);

        // Attempt 2: re-run stage with backup source active in context.
        var backupResult = await runStage(cancellationToken).ConfigureAwait(false);

        if (!backupResult.IsSuccess)
        {
            ResiliencyLog.AttemptFailed(
                pbCtx.Logger,
                ctx.ExecutionId,
                2,
                backupResult.CurrentMessage ?? "Backup stage failed");
        }

        // Schedule the refresh pipeline to re-sync primary, regardless of backup result.
        var scheduleResult = await pbCtx.ScheduleClient.ToggleSchedule(
                pbConfig.RefreshScheduleId.ToString("N"), cancellationToken)
            .ConfigureAwait(false);

        if (!scheduleResult.IsSuccess)
        {
            PrimaryBackupLog.ScheduleToggleFailed(
                pbCtx.Logger,
                ctx.ExecutionId,
                pbConfig.RefreshScheduleId,
                scheduleResult.CurrentMessage ?? "unknown");
        }

        return backupResult;
    }
}
