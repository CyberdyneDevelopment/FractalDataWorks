using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Resiliency.RetryNotify;

/// <summary>
/// MessageLogging methods for RetryNotify resiliency strategy.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("RETRYNOTIFY")]
public static partial class RetryNotifyLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // RetryNotify Strategy Events (7120-7129)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the wrong configuration type is passed to RetryNotify.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "RetryNotify received wrong configuration type: expected RetryNotifyResiliencyConfiguration, got '{configType}'")]
    public static partial IGenericMessage WrongConfigurationType(
        ILogger logger,
        string configType);

    /// <summary>
    /// Logs when the execution context is not IRetryNotifyResiliencyContext.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "RetryNotify requires IRetryNotifyResiliencyContext: executionId={executionId}, contextType='{contextType}'")]
    public static partial IGenericMessage WrongContextType(
        ILogger logger,
        Guid executionId,
        string contextType);

    /// <summary>
    /// Logs when all retries are exhausted and the terminal failure result is returned.
    /// </summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Error,
        Message = "RetryNotify exhausted all attempts: executionId={executionId}, maxRetries={maxRetries}, reason='{reason}'")]
    public static partial IGenericMessage RetriesExhausted(
        ILogger logger,
        Guid executionId,
        int maxRetries,
        string reason);

    /// <summary>
    /// Logs when a RetryNotify policy leaves out a value its definer must state. Nothing assumes one (FDW-3931).
    /// </summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "RetryNotify policy '{policyName}' does not state {field}; whoever defines the policy states every value")]
    public static partial IGenericMessage PolicyValueNotStated(
        ILogger logger,
        string policyName,
        string field);

    /// <summary>
    /// Logs when a RetryNotify policy names a backoff kind that is not Exponential, Fixed or Random. No kind is assumed for it (FDW-3931).
    /// </summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "RetryNotify policy '{policyName}' names backoff kind '{backoffKind}'; the kinds are Exponential, Fixed and Random")]
    public static partial IGenericMessage BackoffKindUnknown(
        ILogger logger,
        string policyName,
        string backoffKind);
}
