namespace Fdw.Web.Http.Authentication.OpenIdConnect.Logging;

using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for OpenID Connect authentication operations.
/// EventId range: 4500-4519
/// </summary>
[MessageLoggingTypeCode("OPENIDCONNECT")]
public static partial class OidcAuthLog
{
    /// <summary>
    /// Logged at Trace level when OIDC provider options are being configured.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Configuring OIDC provider '{providerName}' with authority '{authority}'")]
    public static partial IGenericMessage ConfiguringProvider(
        ILogger logger,
        string providerName,
        string authority);

    /// <summary>
    /// Logged at Debug level when OIDC authentication services are registered.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Registered OIDC authentication for provider '{providerName}'")]
    public static partial IGenericMessage ProviderRegistered(
        ILogger logger,
        string providerName);

    /// <summary>
    /// Logged at Information level when redirecting to the provider's password recovery flow.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Redirecting to '{providerName}' password recovery at '{recoveryUrl}'")]
    public static partial IGenericMessage RedirectingToRecovery(
        ILogger logger,
        string providerName,
        string recoveryUrl);

    /// <summary>
    /// Logged at Warning level when no recovery URL is configured for the provider.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "No password recovery URL configured for OIDC provider '{providerName}'")]
    public static partial IGenericMessage NoRecoveryUrlConfigured(
        ILogger logger,
        string providerName);

    /// <summary>
    /// Logged at Information level when Authentik-specific defaults are applied.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Registered Authentik OIDC authentication with authority '{authority}'")]
    public static partial IGenericMessage AuthentikRegistered(
        ILogger logger,
        string authority);
}
