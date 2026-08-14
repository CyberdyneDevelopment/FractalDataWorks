using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Logging;

/// <summary>
/// MessageLogging for the managed identity domain.
/// </summary>
/// <remarks>
/// <para>
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>) drawn from this package's open
/// band: 11xxx non-error, 51xxx auth, 61xxx configuration, 71xxx dependency/IO, 91xxx internal.
/// </para>
/// <para>
/// <b>No method here takes a token value.</b> An issued token is a bearer credential, and a log that
/// carries one turns every log sink into a place the service can be impersonated from. Audience,
/// scopes, issuer, expiry and configuration name are what an operator needs to diagnose an
/// authorization failure, and none of them is a credential.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("IDENTITY")]
public static partial class IdentityLog
{
    /// <summary>Logs that a token acquisition is starting.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration being used.</param>
    /// <param name="mechanism">The identity mechanism (the ServiceOptionType).</param>
    /// <param name="audience">The audience the token is being requested for.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Acquiring identity token: configuration '{configurationName}' via {mechanism} for audience '{audience}'")]
    public static partial IGenericMessage AcquiringToken(ILogger logger, string configurationName, string mechanism, string audience);

    /// <summary>Logs that a token was issued.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority.</param>
    /// <param name="audience">The audience the token is valid at.</param>
    /// <param name="scopes">The scopes actually granted.</param>
    /// <param name="expiresAt">When the token stops being valid.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Identity token issued: configuration '{configurationName}' from '{issuer}' for audience '{audience}', scopes [{scopes}], expires {expiresAt:O}")]
    public static partial IGenericMessage TokenIssued(ILogger logger, string configurationName, string issuer, string audience, string scopes, DateTimeOffset expiresAt);

    /// <summary>Logs that a live cached token was reused instead of acquiring a new one.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="audience">The audience the cached token is valid at.</param>
    /// <param name="expiresAt">When the cached token stops being valid.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Reusing cached identity token: configuration '{configurationName}' for audience '{audience}', expires {expiresAt:O}")]
    public static partial IGenericMessage TokenServedFromCache(ILogger logger, string configurationName, string audience, DateTimeOffset expiresAt);

    /// <summary>Logs that a cached token was dropped.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration whose entry was dropped.</param>
    /// <param name="audience">The audience of the dropped entry.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug, Message = "Dropped cached identity token: configuration '{configurationName}' for audience '{audience}'")]
    public static partial IGenericMessage TokenCacheInvalidated(ILogger logger, string configurationName, string audience);

    /// <summary>Logs that the identity domain registered an option.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mechanism">The identity mechanism registered.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Identity mechanism registered: {mechanism}")]
    public static partial IGenericMessage MechanismRegistered(ILogger logger, string mechanism);

    /// <summary>Logs that the outbound access-token bridge attached a managed-identity token to a request.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="audience">The audience the token is valid at.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Outbound request carrying managed identity: configuration '{configurationName}' for audience '{audience}'")]
    public static partial IGenericMessage OutboundTokenAttached(ILogger logger, string configurationName, string audience);

    /// <summary>Logs that the identity provider rejected this service's credential.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority that rejected the credential.</param>
    /// <param name="error">The error the provider reported.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 51000, Level = LogLevel.Error, Message = "Identity provider rejected this service's credential: configuration '{configurationName}' at '{issuer}' — {error}")]
    public static partial IGenericMessage CredentialRejected(ILogger logger, string configurationName, string issuer, string error);

    /// <summary>Logs that the identity provider granted narrower scopes than were requested.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="requested">The scopes requested.</param>
    /// <param name="granted">The scopes actually granted.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning, Message = "Identity token granted narrower scopes than requested: configuration '{configurationName}' requested [{requested}], granted [{granted}]")]
    public static partial IGenericMessage ScopesNarrowed(ILogger logger, string configurationName, string requested, string granted);

    /// <summary>Logs that no identity configuration exists under the requested name.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The name that was asked for.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "No identity configuration named '{configurationName}' exists")]
    public static partial IGenericMessage ConfigurationNotFound(ILogger logger, string configurationName);

    /// <summary>Logs that an identity configuration is missing a value it cannot run without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration.</param>
    /// <param name="property">The property that has no value.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61001, Level = LogLevel.Critical, Message = "Identity configuration '{configurationName}' is missing required value '{property}'")]
    public static partial IGenericMessage ConfigurationValueMissing(ILogger logger, string configurationName, string property);

    /// <summary>Logs that a header row's typed body did not load.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration.</param>
    /// <param name="mechanism">The ServiceOptionType the header declared.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61002, Level = LogLevel.Critical, Message = "Identity configuration '{configurationName}' declares mechanism '{mechanism}' but its typed configuration body did not load")]
    public static partial IGenericMessage TypedBodyMissing(ILogger logger, string configurationName, string mechanism);

    /// <summary>Logs that the federated assertion this mechanism depends on was not present.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration.</param>
    /// <param name="source">The assertion source that was consulted.</param>
    /// <param name="location">Where the source looked (e.g. an environment variable name or file path).</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error, Message = "Identity configuration '{configurationName}' found no federated assertion via {source} at '{location}'")]
    public static partial IGenericMessage AssertionNotAvailable(ILogger logger, string configurationName, string source, string location);

    /// <summary>Logs that the federated assertion existed but could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The read exception.</param>
    /// <param name="configurationName">The identity configuration.</param>
    /// <param name="source">The assertion source that was consulted.</param>
    /// <param name="location">Where the source looked.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Identity configuration '{configurationName}' could not read its federated assertion via {source} at '{location}'")]
    public static partial IGenericMessage AssertionUnreadable(ILogger logger, Exception ex, string configurationName, string source, string location);

    /// <summary>Logs that the identity provider could not be reached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The transport exception.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority that could not be reached.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Could not reach identity provider for configuration '{configurationName}' at '{issuer}'")]
    public static partial IGenericMessage ProviderUnreachable(ILogger logger, Exception ex, string configurationName, string issuer);

    /// <summary>Logs that the identity provider returned a non-success HTTP status.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority.</param>
    /// <param name="statusCode">The HTTP status returned.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error, Message = "Identity provider returned {statusCode} for configuration '{configurationName}' at '{issuer}'")]
    public static partial IGenericMessage ProviderReturnedError(ILogger logger, string configurationName, string issuer, int statusCode);

    /// <summary>Logs that the identity provider's token response could not be read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The parse exception.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Could not read the token response from identity provider for configuration '{configurationName}' at '{issuer}'")]
    public static partial IGenericMessage TokenResponseUnreadable(ILogger logger, Exception ex, string configurationName, string issuer);

    /// <summary>Logs that the identity provider's token response was well-formed but incomplete.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <param name="issuer">The issuing authority.</param>
    /// <param name="field">The field the response did not carry.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Identity provider response for configuration '{configurationName}' at '{issuer}' carried no '{field}'")]
    public static partial IGenericMessage TokenResponseIncomplete(ILogger logger, string configurationName, string issuer, string field);

    /// <summary>Logs that a provider error response was not the JSON the OAuth spec calls for.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The parse exception.</param>
    /// <param name="configurationName">The identity configuration used.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning, Message = "Identity provider error response for configuration '{configurationName}' was not JSON — the request may not have reached the token endpoint")]
    public static partial IGenericMessage ErrorResponseUnparseable(ILogger logger, Exception ex, string configurationName);

    /// <summary>Logs that the generic Execute surface was asked for a type this service does not return.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requestedType">The type the caller asked for.</param>
    /// <param name="actualType">The type this service actually returns.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Identity token requested as '{requestedType}' but this service returns '{actualType}'")]
    public static partial IGenericMessage ResultTypeMismatch(ILogger logger, string requestedType, string actualType);
}
