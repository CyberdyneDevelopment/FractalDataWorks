using Fdw.Configuration;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Resiliency;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency.RetryNotify;

/// <summary>
/// RetryNotify resiliency strategy. Retries N times with configurable backoff;
/// on terminal failure, publishes a notification via the configured target.
/// </summary>
[TypeOption(typeof(ResiliencyTypes), "RetryNotify")]
public sealed class RetryNotifyResiliencyType : ResiliencyTypeBase
{
    // The backoff kinds a policy may name; any other is refused, never read as Exponential (FDW-3931).
    private static readonly System.Collections.Generic.HashSet<string> BackoffKinds =
        new(StringComparer.OrdinalIgnoreCase) { "Exponential", "Fixed", "Random" };

    /// <summary>Initializes a new instance of <see cref="RetryNotifyResiliencyType"/>.</summary>
    public RetryNotifyResiliencyType()
        : base(
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

        var refusal = Refusal(rnConfig, rnCtx.Logger);
        if (refusal is not null)
        {
            return GenericResult.Failure(refusal);
        }

        var maxRetries = rnConfig.MaxRetries!.Value;
        IGenericResult? lastResult = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lastResult = await runStage(cancellationToken).ConfigureAwait(false);

            if (lastResult.IsSuccess)
                return lastResult;

            if (attempt < maxRetries)
            {
                ResiliencyLog.AttemptFailed(
                    rnCtx.Logger, ctx.ExecutionId, attempt,
                    lastResult.FailureReason().Message);

                // Apply backoff delay before the next attempt.
                var delay = CalculateDelay(rnConfig, attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // Why before the notification: a MaxRetries below zero runs no attempt, so no stage failed and there is nothing to
        // notify about. The loop returns on success, so a result here is the last attempt's failure.
        if (lastResult is null)
        {
            return GenericResult.Failure(
                RetryNotifyLog.RetriesExhausted(
                    rnCtx.Logger, ctx.ExecutionId, maxRetries, "Stage failed after max retries"));
        }

        if (lastResult.IsSuccess)
            return lastResult;

        // Terminal failure — all retries exhausted.
        ResiliencyLog.MaxRetriesExceeded(rnCtx.Logger, ctx.ExecutionId, maxRetries);

        // Send notification.
        await rnCtx.NotifyTerminalFailure(
            rnConfig.NotificationTargetId,
            ctx.ExecutionId,
            lastResult.FailureReason().Message,
            cancellationToken).ConfigureAwait(false);

        ResiliencyLog.NotificationSent(rnCtx.Logger, ctx.ExecutionId, rnConfig.NotificationTargetId);

        return lastResult;
    }

    // Why a refusal: every value of a policy is its definer's to state (Mike, FDW-3931: "you are defining a resilience policy. why in the
    // fuck would you have a default?"). The first one left out, or a backoff kind that is not one, is named.
    private static IGenericMessage? Refusal(RetryNotifyResiliencyConfiguration config, ILogger logger)
    {
        var unstated = config.MaxRetries is null ? nameof(config.MaxRetries)
            : config.BackoffKind is null ? nameof(config.BackoffKind)
            : config.BaseDelayMs is null ? nameof(config.BaseDelayMs)
            : config.MaxDelayMs is null ? nameof(config.MaxDelayMs)
            : null;
        if (unstated is not null)
        {
            return RetryNotifyLog.PolicyValueNotStated(logger, config.Name, unstated);
        }

        return BackoffKinds.Contains(config.BackoffKind!) ? null : RetryNotifyLog.BackoffKindUnknown(logger, config.Name, config.BackoffKind!);
    }

    // Refusal has checked every value and the kind before a delay is worked out.
    private static TimeSpan CalculateDelay(RetryNotifyResiliencyConfiguration config, int attempt)
    {
        var baseMs = config.BaseDelayMs!.Value;
        if (string.Equals(config.BackoffKind, "Fixed", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMilliseconds(baseMs);

        if (string.Equals(config.BackoffKind, "Random", StringComparison.OrdinalIgnoreCase))
        {
            var jitter = RandomNumberGenerator.GetInt32(0, Math.Max(1, baseMs));
            return TimeSpan.FromMilliseconds(baseMs + jitter);
        }

        var exponentialMs = (int)Math.Min(Math.Pow(2, attempt) * baseMs, config.MaxDelayMs!.Value);
        return TimeSpan.FromMilliseconds(exponentialMs);
    }
}
