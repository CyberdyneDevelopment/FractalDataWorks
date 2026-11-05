using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Resiliency.Polly;

/// <summary>
/// MessageLogging methods for PollyRetry resiliency strategy.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("POLLY")]
public static partial class PollyRetryLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // PollyRetry Strategy Events (7100-7109)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the wrong configuration type is passed to PollyRetry.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "PollyRetry received wrong configuration type: expected PollyRetryResiliencyConfiguration, got '{configType}'")]
    public static partial IGenericMessage WrongConfigurationType(
        ILogger logger,
        string configType);

    /// <summary>
    /// Logs when all Polly retries are exhausted and the stage ultimately fails.
    /// </summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Error,
        Message = "PollyRetry exhausted all attempts: executionId={executionId}, maxRetries={maxRetries}, reason='{reason}'")]
    public static partial IGenericMessage RetriesExhausted(
        ILogger logger,
        Guid executionId,
        int maxRetries,
        string reason);

    /// <summary>
    /// Logs when a PollyRetry policy leaves out a value its definer must state. Nothing assumes one (FDW-3931).
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "PollyRetry policy '{policyName}' does not state {field}; whoever defines the policy states every value")]
    public static partial IGenericMessage PolicyValueNotStated(
        ILogger logger,
        string policyName,
        string field);

    /// <summary>
    /// Logs when a PollyRetry policy names a backoff kind that is not Exponential, Fixed or Random. No kind is assumed for it (FDW-3931).
    /// </summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "PollyRetry policy '{policyName}' names backoff kind '{backoffKind}'; the kinds are Exponential, Fixed and Random")]
    public static partial IGenericMessage BackoffKindUnknown(
        ILogger logger,
        string policyName,
        string backoffKind);
}
