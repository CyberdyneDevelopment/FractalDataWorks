using Fdw.Configuration;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Resiliency;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency.RetryNotify;

/// <summary>
/// RetryNotify resiliency strategy. Retries N times with configurable backoff;
/// on terminal failure, publishes a notification via the configured target.
/// </summary>
[TypeOption(typeof(ResiliencyTypes), "RetryNotify")]
public sealed class RetryNotifyResiliencyType : ResiliencyTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RetryNotifyResiliencyType"/>.</summary>
    public RetryNotifyResiliencyType()
        : base(
            id: 4,
            name: "RetryNotify",
            displayName: "Retry with Notification",
            description: "N retries with backoff, then notify on terminal failure.")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        if (runStage == null) throw new ArgumentNullException(nameof(runStage));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        if (config is not RetryNotifyResiliencyConfiguration rnConfig)
        {
            return GenericResult.Failure(
                RetryNotifyLog.WrongConfigurationType(NullLogger.Instance, config.GetType().Name));
        }

        if (ctx is not IRetryNotifyResiliencyContext rnCtx)
        {
            return GenericResult.Failure(
                RetryNotifyLog.WrongContextType(NullLogger.Instance, ctx.ExecutionId, ctx.GetType().Name));
        }

        IGenericResult? lastResult = null;

        for (var attempt = 0; attempt <= rnConfig.MaxRetries; attempt++)
        {
            // Why: use ThrowIfCancellationRequested per CA2250 — more idiomatic than manual check+throw.
            cancellationToken.ThrowIfCancellationRequested();

            lastResult = await runStage(cancellationToken).ConfigureAwait(false);

            if (lastResult.IsSuccess)
                return lastResult;

            if (attempt < rnConfig.MaxRetries)
            {
                ResiliencyLog.AttemptFailed(
                    rnCtx.Logger, ctx.ExecutionId, attempt,
                    lastResult.CurrentMessage ?? "stage failed");

                // Apply backoff delay before the next attempt.
                var delay = CalculateDelay(rnConfig, attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // Terminal failure — all retries exhausted.
        ResiliencyLog.MaxRetriesExceeded(rnCtx.Logger, ctx.ExecutionId, rnConfig.MaxRetries);

        // Send notification.
        await rnCtx.NotifyTerminalFailure(
            rnConfig.NotificationTargetId,
            ctx.ExecutionId,
            lastResult?.CurrentMessage ?? "Stage failed after max retries",
            cancellationToken).ConfigureAwait(false);

        ResiliencyLog.NotificationSent(rnCtx.Logger, ctx.ExecutionId, rnConfig.NotificationTargetId);

        // Why: lastResult is always non-null here because the loop always runs at least once
        // (MaxRetries >= 0 ensures one attempt). Return via explicit check to satisfy FDW012.
        if (lastResult is not null)
            return lastResult;

        return GenericResult.Failure(
            RetryNotifyLog.RetriesExhausted(
                rnCtx.Logger, ctx.ExecutionId, rnConfig.MaxRetries, "Stage failed after max retries"));
    }

    private static TimeSpan CalculateDelay(RetryNotifyResiliencyConfiguration config, int attempt)
    {
        var baseMs = config.BaseDelayMs;
        if (string.Equals(config.BackoffKind, "Fixed", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMilliseconds(baseMs);

        if (string.Equals(config.BackoffKind, "Random", StringComparison.OrdinalIgnoreCase))
        {
            // Why: use RandomNumberGenerator.GetInt32 to avoid SCS0005 — SecurityCodeScan flags
            // System.Random regardless of use-case; cryptographic RNG satisfies the analyzer.
            var jitter = RandomNumberGenerator.GetInt32(0, Math.Max(1, baseMs));
            return TimeSpan.FromMilliseconds(baseMs + jitter);
        }

        // Why: default exponential — 2^attempt * baseMs, capped at 30 seconds.
        var exponentialMs = (int)Math.Min(Math.Pow(2, attempt) * baseMs, 30000);
        return TimeSpan.FromMilliseconds(exponentialMs);
    }
}
