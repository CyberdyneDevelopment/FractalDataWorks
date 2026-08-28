using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging for minting tokens.
/// </summary>
/// <remarks>
/// EventId range: 91180–91184. The token itself never appears here. It is a bearer credential, and
/// anything that reaches a log reaches whoever can read logs.
/// </remarks>
[MessageLoggingTypeCode("TOKENMANAGER")]
internal static partial class IssuerLog
{
    /// <summary>A token was minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it was minted for.</param>
    /// <param name="principalId">Who it names.</param>
    /// <param name="acr">The assurance the flow reached.</param>
    [MessageLogging(EventId = 91180, Level = LogLevel.Information,
        Message = "Issued a token for audience '{audience}' naming principal {principalId} at assurance '{acr}'")]
    internal static partial IGenericMessage Issued(
        ILogger<JwtTokenIssuer> logger, string audience, Guid principalId, string acr);

    /// <summary>The signing key was fetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    [MessageLogging(EventId = 91183, Level = LogLevel.Debug,
        Message = "Loaded signing key '{keyName}'")]
    internal static partial IGenericMessage SigningKeyLoaded(
        ILogger<SecretManagerSigningCredentialProvider> logger, string keyName);

    /// <summary>The signing key could not be parsed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    /// <param name="exception">What the parser reported.</param>
    // Critical, not Error: Without a signing key nothing can be minted, by any flow, and no retry recovers it. The service is running and cannot do the
    // one thing it exists for.
    [MessageLogging(EventId = 91184, Level = LogLevel.Critical,
        Message = "Signing key '{keyName}' is not a readable PEM private key")]
    internal static partial IGenericMessage SigningKeyUnreadable(
        ILogger<SecretManagerSigningCredentialProvider> logger, Exception exception, string keyName);

    /// <summary>A token is about to be minted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="audience">Who it is for.</param>
    /// <param name="claimCount">How many claims it will carry.</param>
    [MessageLogging(EventId = 91185, Level = LogLevel.Trace,
        Message = "Minting a token for '{audience}' carrying {claimCount} claim(s)")]
    internal static partial IGenericMessage Minting(
        ILogger<JwtTokenIssuer> logger, string audience, int claimCount);

    /// <summary>The cached signing key was reused rather than refetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">Its name in the secret manager.</param>
    [MessageLogging(EventId = 91186, Level = LogLevel.Trace,
        Message = "Reused the cached signing key '{keyName}'")]
    internal static partial IGenericMessage SigningKeyReused(
        ILogger<SecretManagerSigningCredentialProvider> logger, string keyName);

    /// <summary>No request was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91181, Level = LogLevel.Error,
        Message = "A request must be supplied to issue a token")]
    internal static partial IGenericMessage RequestMissing(ILogger<JwtTokenIssuer> logger);

    /// <summary>The request named no audience.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91182, Level = LogLevel.Error,
        Message = "A token cannot be issued without an audience")]
    internal static partial IGenericMessage AudienceMissing(ILogger<JwtTokenIssuer> logger);
}
