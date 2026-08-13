using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Aegis.Logging;

/// <summary>
/// MessageLogging for Aegis Gateway operations. Every log message is returned in the result AND
/// logged (log-and-return).
/// </summary>
/// <remarks>
/// Why: EventIds here deliberately match the corresponding <c>AegisResultCodes</c> catalog numbers
/// one-for-one (both draw from the "AEG" prefix pool) — a failure logged via
/// <see cref="SecretResolutionFailed(ILogger, string, string)"/> carries the identical <c>Code</c>
/// ("AEG-71000") as <c>AegisResultCodes.SecretResolutionFailed</c>, so the log line and the returned
/// <c>IGenericResult</c> failure are traceable to the same condition without a second lookup.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("AEG")]
public static partial class AegisLog
{
    // ── Category 1 (10000–19999): non-error operational trace ────────────────────────────────────
    // Why 11000+: 10000–10999 is the FDW-reserved canonical band (Succeeded/Informational/Cancelled);
    // 11000–19999 is the open per-package custom band, which is where the info-heavy operational
    // EventIds belong. These are log-only — they are NOT AegisResultCodes, because a non-failure
    // trace point is never a returnable result.

    /// <summary>Logs that the stdio server finished bootstrapping and is ready to serve tools.</summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Aegis MCP stdio server ready: {commandCount} declared command(s), {connectionCount} declared connection(s).")]
    public static partial IGenericMessage ServerReady(ILogger logger, int commandCount, int connectionCount);

    /// <summary>Logs entry into an MCP tool method.</summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Tool '{toolName}' invoked.")]
    public static partial IGenericMessage ToolInvoked(ILogger logger, string toolName);

    /// <summary>Logs the result of the list_connections tool.</summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Listed {commandCount} declared command(s).")]
    public static partial IGenericMessage ConnectionsListed(ILogger logger, int commandCount);

    /// <summary>Logs the result of the describe_action tool.</summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Described command '{commandName}' with {parameterCount} declared parameter(s).")]
    public static partial IGenericMessage ActionDescribed(ILogger logger, string commandName, int parameterCount);

    /// <summary>Logs the start of a request_action brokered execution.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Action requested: connection '{connectionName}', command '{commandName}' (correlation {correlationId}).")]
    public static partial IGenericMessage ActionRequested(ILogger logger, string connectionName, string commandName, Guid correlationId);

    /// <summary>Logs that every submitted parameter passed the declared allow-list.</summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Parameters validated for command '{commandName}': {parameterCount} submitted.")]
    public static partial IGenericMessage ParametersValidated(ILogger logger, string commandName, int parameterCount);

    /// <summary>Logs the approval verdict reached for a request.</summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Verdict '{disposition}' for command '{commandName}' (correlation {correlationId}).")]
    public static partial IGenericMessage VerdictReached(ILogger logger, string disposition, string commandName, Guid correlationId);

    /// <summary>Logs that injection is about to begin for an approved request.</summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "Injection starting for command '{commandName}' (correlation {correlationId}).")]
    public static partial IGenericMessage InjectionStarting(ILogger logger, string commandName, Guid correlationId);

    /// <summary>Logs that injection completed and returned a sanitized outcome.</summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Injection succeeded for command '{commandName}' (correlation {correlationId}).")]
    public static partial IGenericMessage InjectionSucceeded(ILogger logger, string commandName, Guid correlationId);

    /// <summary>
    /// Logs that the declared secret manager was resolved. Deliberately carries the MANAGER name
    /// only — never the secret key name, and never the value.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Secret manager '{secretManagerName}' resolved for injection.")]
    public static partial IGenericMessage SecretManagerResolved(ILogger logger, string secretManagerName);

    /// <summary>Logs which declared approval policy the evaluator matched.</summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Policy '{policyType}' evaluated for command '{commandName}'.")]
    // Why string?: AegisCommandConfiguration.ServiceOptionType is nullable (a config row may not carry
    // a discriminator), and this is a diagnostic line, not a decision point. Logging the absence
    // truthfully is correct here — substituting a placeholder would report a policy kind that was
    // never configured. The approval decision below is unaffected: string.Equals(null, "PreApproved")
    // is false, so a row without a discriminator stays denied, fail-closed.
    public static partial IGenericMessage PolicyEvaluated(ILogger logger, string? policyType, string commandName);

    // ── Categories 2–9: failure codes, one-for-one with AegisResultCodes ──────────────────────────

    /// <summary>Logs that a required value was not provided.</summary>
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Error,
        Message = "Required value '{name}' was not provided.")]
    public static partial IGenericMessage RequiredValueMissing(ILogger logger, string name);

    // ── Host registration phases ────────────────────────────────────────────────────────────────
    // Why this host logs its phases at all: it registers secret managers and nothing else, so when a
    // phase fails silently the first symptom is a secret that will not resolve — a runtime error far
    // from the startup step that actually broke. Volume thins as severity rises: Trace names each
    // phase as it begins, Debug reports it completing, Critical fires when one fails, because a
    // failed phase means this host cannot serve a single secret for the rest of its life.

    /// <summary>Trace: one line per phase as it begins. The finest grain — three lines per startup.</summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Trace,
        Message = "Aegis host phase '{phase}' starting")]
    public static partial IGenericMessage HostPhaseStarting(ILogger logger, string phase);

    /// <summary>Debug: the phase finished and the host may proceed to the next one.</summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Aegis host phase '{phase}' completed")]
    public static partial IGenericMessage HostPhaseCompleted(ILogger logger, string phase);

    /// <summary>
    /// Critical: the phase failed, so this host's secret managers are not registered and no secret
    /// can be resolved for the lifetime of the process. Carries the underlying reason rather than
    /// restating the phase, because the inner failure already names what actually broke.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Critical,
        Message = "Aegis host phase '{phase}' FAILED — secret managers are not registered and no secret can be resolved: {reason}")]
    public static partial IGenericMessage HostPhaseFailed(ILogger logger, string phase, string? reason);

    /// <summary>Logs that a submitted parameter is not permitted by the command's allow-list.</summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Parameter '{parameterName}' is not permitted for command '{commandName}'.")]
    public static partial IGenericMessage ParameterNotInAllowList(ILogger logger, string parameterName, string commandName);

    /// <summary>Logs that the requested command is not declared for the given connection.</summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Command '{commandName}' is not declared for connection '{connectionName}'.")]
    public static partial IGenericMessage ConnectionNotDeclared(ILogger logger, string connectionName, string commandName);

    /// <summary>Logs that the approval policy denied the requested action.</summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Action '{commandName}' was denied: {reason}")]
    public static partial IGenericMessage ActionDenied(ILogger logger, string commandName, string reason);

    /// <summary>Logs that secret resolution failed during injection.</summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to resolve secret '{secretKeyName}' from manager '{secretManagerName}'.")]
    public static partial IGenericMessage SecretResolutionFailed(ILogger logger, string secretManagerName, string secretKeyName);

    /// <summary>Logs that the downstream injection call failed after the secret was resolved.</summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Injection failed for command '{commandName}': {reason}")]
    public static partial IGenericMessage InjectionFailed(ILogger logger, string commandName, string reason);
}
