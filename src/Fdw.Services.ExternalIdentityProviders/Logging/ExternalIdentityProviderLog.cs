using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Logging;

/// <summary>
/// MessageLogging methods for the ExternalIdentityProviders domain: identity binding
/// (<c>ExternalIdentityBinding</c>) and JIT provisioning (each <c>IExternalIdentityProvisioner</c>
/// implementation, e.g. <c>ChainedExternalIdentityProvisioner</c>/<c>ClaimMappedProvisioner</c>). Every
/// log message is returned in the result AND logged.
/// EventId range: 7457-7466 (see EVENTID-ALLOCATION.md).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("EXTIDPROVIDER")]
public static partial class ExternalIdentityProviderLog
{
    /// <summary>Logs the start of external token validation.</summary>
    [MessageLogging(
        EventId = 7457,
        Level = LogLevel.Trace,
        Message = "External token validation started for provider '{providerName}'")]
    public static partial IGenericMessage ValidationStarted(ILogger logger, string providerName);

    /// <summary>Logs that external token validation succeeded.</summary>
    [MessageLogging(
        EventId = 7458,
        Level = LogLevel.Information,
        Message = "External token validated successfully for provider '{providerName}': subject='{subject}'")]
    public static partial IGenericMessage ValidationSucceeded(ILogger logger, string providerName, string subject);

    /// <summary>Logs that external token validation failed.</summary>
    [MessageLogging(
        EventId = 7459,
        Level = LogLevel.Warning,
        Message = "External token validation failed for provider '{providerName}': {reason}")]
    public static partial IGenericMessage ExternalTokenValidationFailed(ILogger logger, string providerName, string reason);

    /// <summary>Logs that a provider's Authority/ClientId configuration is incomplete.</summary>
    [MessageLogging(
        EventId = 7460,
        Level = LogLevel.Error,
        Message = "External identity provider '{providerName}' configuration is incomplete: {reason}")]
    public static partial IGenericMessage ConfigurationIncomplete(ILogger logger, string providerName, string reason);

    /// <summary>Logs the start of external identity provider resolution.</summary>
    [MessageLogging(
        EventId = 7461,
        Level = LogLevel.Trace,
        Message = "Resolving external identity provider (requested name='{requestedName}')")]
    public static partial IGenericMessage ResolvingProvider(ILogger logger, string requestedName);

    /// <summary>Logs that an external identity provider was resolved.</summary>
    [MessageLogging(
        EventId = 7462,
        Level = LogLevel.Information,
        Message = "Resolved external identity provider '{providerName}'")]
    public static partial IGenericMessage ProviderResolved(ILogger logger, string providerName);

    /// <summary>
    /// Logs that no external identity provider could be resolved — covers both "no active
    /// configurations exist" and "an explicit 'provider' parameter did not resolve to an active
    /// configuration" (and "more than one active configuration exists without a 'provider' parameter").
    /// </summary>
    [MessageLogging(
        EventId = 7463,
        Level = LogLevel.Error,
        Message = "No external identity provider available: {reason}")]
    public static partial IGenericMessage ExternalIdentityProviderNotConfigured(ILogger logger, string reason);

    /// <summary>Logs that a command was routed to <c>IGenericService.Execute</c>, which this domain never dispatches through.</summary>
    [MessageLogging(
        EventId = 7464,
        Level = LogLevel.Error,
        Message = "Command '{commandType}' is not dispatchable from IExternalIdentityProvider.Execute — validation happens via ValidateExternalToken.")]
    public static partial IGenericMessage CommandNotDispatchable(ILogger logger, string commandType);

    /// <summary>Logs that an external identity provider factory failed to create a service instance.</summary>
    [MessageLogging(
        EventId = 7465,
        Level = LogLevel.Error,
        Message = "External identity provider factory failed to create service for configName='{configName}': {message}")]
    public static partial IGenericMessage FactoryCreateFailed(ILogger logger, string configName, string message);

    /// <summary>Logs that an external identity provider ServiceTypeOption completed registration.</summary>
    [MessageLogging(
        EventId = 7466,
        Level = LogLevel.Information,
        Message = "External identity provider registered: serviceOptionType='{serviceOptionType}'.")]
    public static partial IGenericMessage ProviderRegistered(ILogger logger, string serviceOptionType);

    /// <summary>Logs that the login-discovery endpoint returned the active external identity providers.</summary>
    [MessageLogging(
        EventId = 7467,
        Level = LogLevel.Information,
        Message = "External identity provider discovery returned {count} active provider(s).")]
    public static partial IGenericMessage ProviderDiscoveryReturned(ILogger logger, int count);

    /// <summary>Logs that the login-discovery endpoint failed to read the active external identity providers.</summary>
    [MessageLogging(
        EventId = 7468,
        Level = LogLevel.Error,
        Message = "External identity provider discovery failed: {reason}")]
    public static partial IGenericMessage ProviderDiscoveryFailed(ILogger logger, string reason);
}
