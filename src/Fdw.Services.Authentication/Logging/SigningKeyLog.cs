using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for fetching an authority's signing keys.
/// </summary>
/// <remarks>EventId range: 91170–91175.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class SigningKeyLog
{
    /// <summary>Keys were fetched and cached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jwksUri">Where they came from.</param>
    /// <param name="keyCount">How many were published.</param>
    [MessageLogging(EventId = 91170, Level = LogLevel.Debug,
        Message = "Fetched {keyCount} signing key(s) from {jwksUri}")]
    internal static partial IGenericMessage Fetched(
        ILogger<CachingSigningKeyProvider> logger, string jwksUri, int keyCount);

    /// <summary>A forced refresh was declined because one happened recently.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jwksUri">The key document.</param>
    // Why Debug and not Warning: throttling is the mechanism working. Seeing it constantly means
    // tokens are arriving signed by a key that does not exist, which the rejections already say.
    [MessageLogging(EventId = 91171, Level = LogLevel.Debug,
        Message = "A refresh of {jwksUri} was throttled; the cached keys were returned")]
    internal static partial IGenericMessage RefreshThrottled(
        ILogger<CachingSigningKeyProvider> logger, string jwksUri);

    /// <summary>The authority published no usable signing keys.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jwksUri">The key document.</param>
    // Why Error: an authority publishing no keys cannot have anything verified against it, and no
    // token from it will ever be accepted until that is fixed.
    [MessageLogging(EventId = 91172, Level = LogLevel.Error,
        Message = "{jwksUri} published no usable signing keys")]
    internal static partial IGenericMessage NoKeysPublished(
        ILogger<CachingSigningKeyProvider> logger, string jwksUri);

    /// <summary>The key document could not be fetched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jwksUri">The key document.</param>
    /// <param name="failure">The kind of failure.</param>
    // Why Error: the identity provider is unreachable, so every login through it fails until it is
    // not. Expected operationally, still an outage.
    [MessageLogging(EventId = 91173, Level = LogLevel.Error,
        Message = "Could not fetch signing keys from {jwksUri}: {failure}")]
    internal static partial IGenericMessage FetchFailed(
        ILogger<CachingSigningKeyProvider> logger, string jwksUri, string failure);

    /// <summary>No key document location was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91174, Level = LogLevel.Error,
        Message = "A key document location must be supplied")]
    internal static partial IGenericMessage UriMissing(ILogger<CachingSigningKeyProvider> logger);
}
