using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Logging;

/// <summary>
/// MessageLogging methods for the ExternalIdentityProvisioners mechanism (<c>ExternalIdentityProvisionerTypes</c>,
/// its factories/TypeOptions) and the provisioner binding selector
/// (<c>ExternalIdentityProvisionerBindingConfigurationProvider</c>). Every log message is returned in the
/// result AND logged. Fresh EventId pool — TypeCode prefix "EXTIDPROVISIONER" is distinct from the
/// sibling ExternalIdentityProviders domain's "EXTIDPROVIDER" prefix (verified no collision by grep).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("EXTIDPROVISIONER")]
public static partial class ExternalIdentityProvisionerLog
{
    // ── IGenericService dispatch ─────────────────────────────────────────────────────

    /// <summary>Logs that a command was routed to <c>IGenericService.Execute</c>, which this domain never dispatches through.</summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Command '{commandType}' is not dispatchable from IExternalIdentityProvisioner.Execute — provisioning happens via Provision.")]
    public static partial IGenericMessage CommandNotDispatchable(ILogger logger, string commandType);

    // ── Factory / registration ────────────────────────────────────────────────────────

    /// <summary>Logs that an external identity provisioner factory failed to create a service instance.</summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "External identity provisioner factory failed to create service for configName='{configName}': {message}")]
    public static partial IGenericMessage FactoryCreateFailed(ILogger logger, string configName, string message);

    /// <summary>Logs that an external identity provisioner Implementation completed registration.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "External identity provisioner registered: implementation='{implementation}'.")]
    public static partial IGenericMessage ProviderRegistered(ILogger logger, string implementation);

    // ── Binding resolution ────────────────────────────────────────────────────────────

    /// <summary>Logs the start of a provisioner binding resolution.</summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "Resolving provisioner binding: tenantId={tenantId} providerName='{providerName}'.")]
    public static partial IGenericMessage ResolvingBinding(ILogger logger, string tenantId, string providerName);

    /// <summary>Logs that a provisioner binding resolved to a named provisioner.</summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Resolved provisioner binding: tenantId={tenantId} providerName='{providerName}' -> provisionerName='{provisionerName}'.")]
    public static partial IGenericMessage BindingResolved(ILogger logger, string tenantId, string providerName, string provisionerName);

    /// <summary>Logs that no provisioner binding matched — provisioning stays default-OFF for this pair.</summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Trace,
        Message = "No provisioner binding found for tenantId={tenantId} providerName='{providerName}' — provisioning stays default-OFF.")]
    public static partial IGenericMessage BindingAbsent(ILogger logger, string tenantId, string providerName);

    /// <summary>Logs that more than one current binding matches the same (tenantId, providerName) pair.</summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Error,
        Message = "Ambiguous provisioner binding: {count} current binding(s) match tenantId={tenantId} providerName='{providerName}'.")]
    public static partial IGenericMessage BindingAmbiguous(ILogger logger, int count, string tenantId, string providerName);

    /// <summary>Logs that reading provisioner binding rows from the gateway failed.</summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Provisioner binding read failed: tenantId={tenantId} providerName='{providerName}'. {message}")]
    public static partial IGenericMessage BindingReadFailed(ILogger logger, string tenantId, string providerName, string message);

    // ── ClaimMapped ────────────────────────────────────────────────────────────────

    /// <summary>Logs that no configured rule matched the presented subject.</summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Trace,
        Message = "ClaimMapped provisioner: no configured rule matched a claim on the subject asserted by '{provider}'.")]
    public static partial IGenericMessage NoRuleMatched(ILogger logger, string provider);

    /// <summary>Logs that a matched rule's UsernameClaimType is absent from the presented claims.</summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "ClaimMapped rule '{ruleName}' matched but its UsernameClaimType '{usernameClaimType}' is not present on the presented claims — cannot name the new account.")]
    public static partial IGenericMessage RuleMissingUsernameClaim(ILogger logger, string ruleName, string usernameClaimType);

    /// <summary>Logs that a matched rule names a role this host has no Role row for.</summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "ClaimMapped rule '{ruleName}' names role '{roleName}', which has no Role row on this host.")]
    public static partial IGenericMessage RuleReferencesUnknownRole(ILogger logger, string ruleName, string roleName);

    /// <summary>Logs that a new account was just-in-time provisioned.</summary>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Information,
        Message = "Subject asserted by '{provider}' provisioned by rule '{ruleName}': new user {userId}.")]
    public static partial IGenericMessage AccountProvisioned(ILogger logger, string provider, string ruleName, Guid userId);

    /// <summary>Logs that provisioning resumed an interrupted prior attempt instead of creating a new user.</summary>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Warning,
        Message = "ClaimMapped rule '{ruleName}': username already exists with no identity link — resuming provisioning for existing user {userId} instead of creating a new one.")]
    public static partial IGenericMessage ResumingOrphanedUser(ILogger logger, string ruleName, Guid userId);

    /// <summary>Logs a provisioned account that cannot be read back by the name it was written under.</summary>
    [MessageLogging(
        EventId = 91007,
        Level = LogLevel.Error,
        Message = "ClaimMapped rule '{ruleName}': the account '{username}' was written but is not readable by that name.")]
    public static partial IGenericMessage ProvisionedUserUnreadable(ILogger logger, string ruleName, string username);
}
