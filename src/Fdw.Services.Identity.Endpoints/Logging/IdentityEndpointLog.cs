using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Endpoints.Logging;

/// <summary>
/// MessageLogging for the managed identity endpoints.
/// </summary>
/// <remarks>
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>) in this package's open band. No
/// method here takes a token or a credential — a verification result is reported by issuer, audience,
/// scopes and expiry, none of which can be used to impersonate the service.
/// </remarks>
[MessageLoggingTypeCode("IDENTITYAPI")]
public static partial class IdentityEndpointLog
{
    /// <summary>Logs that the configured identities were listed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many identities were returned.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Listed {count} configured identities")]
    public static partial IGenericMessage IdentitiesListed(ILogger logger, int count);

    /// <summary>Logs that the available mechanisms were listed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">How many mechanisms are registered.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace, Message = "Listed {count} registered identity mechanisms")]
    public static partial IGenericMessage MechanismsListed(ILogger logger, int count);

    /// <summary>Logs that a verification is starting.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The identity being verified.</param>
    /// <param name="audience">The audience being requested.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Verifying identity '{name}' against audience '{audience}'")]
    public static partial IGenericMessage VerifyingIdentity(ILogger logger, string name, string audience);

    /// <summary>Logs that an identity proved itself.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The identity that was verified.</param>
    /// <param name="issuer">The issuer that answered.</param>
    /// <param name="audience">The audience the token was issued for.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Identity '{name}' verified against '{issuer}' for audience '{audience}'")]
    public static partial IGenericMessage IdentityVerified(ILogger logger, string name, string issuer, string audience);

    /// <summary>Logs that an identity could not prove itself.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The identity that failed.</param>
    /// <param name="audience">The audience that was requested.</param>
    /// <param name="reason">The structured reason from the domain.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning, Message = "Identity '{name}' could not obtain a token for audience '{audience}': {reason}")]
    public static partial IGenericMessage IdentityVerificationFailed(ILogger logger, string name, string audience, string reason);

    /// <summary>Logs that a verification request omitted a value it cannot run without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="property">The property that was not supplied.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "Identity verification request is missing '{property}'")]
    public static partial IGenericMessage VerifyRequestIncomplete(ILogger logger, string property);
}
