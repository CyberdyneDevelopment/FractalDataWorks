using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the authorization request store.
/// </summary>
/// <remarks>EventId range: 91220–91223. State is never logged — it addresses a pending exchange.</remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class RequestStoreLog
{
    /// <summary>An authorization request was stored against its state.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider the request was made to.</param>
    [MessageLogging(EventId = 91224, Level = LogLevel.Trace,
        Message = "Stored a pending authorization request for '{issuer}'")]
    internal static partial IGenericMessage Stored(
        ILogger<InMemoryAuthorizationRequestStore> logger, string issuer);

    /// <summary>A pending request was consumed and its flow continues.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The provider it was made to.</param>
    [MessageLogging(EventId = 91225, Level = LogLevel.Trace,
        Message = "Consumed the pending authorization request for '{issuer}'")]
    internal static partial IGenericMessage Consumed(
        ILogger<InMemoryAuthorizationRequestStore> logger, string issuer);

    /// <summary>No state was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91220, Level = LogLevel.Error,
        Message = "A state value is required to store or consume an authorization request")]
    internal static partial IGenericMessage StateMissing(
        ILogger<InMemoryAuthorizationRequestStore> logger);

    /// <summary>No request was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91221, Level = LogLevel.Error,
        Message = "An authorization request is required to store one")]
    internal static partial IGenericMessage RequestMissing(
        ILogger<InMemoryAuthorizationRequestStore> logger);

    /// <summary>A state value was already in use.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91222, Level = LogLevel.Error,
        Message = "A state value was reused; refusing to overwrite the pending request")]
    internal static partial IGenericMessage StateReused(
        ILogger<InMemoryAuthorizationRequestStore> logger);

    /// <summary>Nothing was pending under the state presented.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91223, Level = LogLevel.Warning,
        Message = "No pending authorization request matches the state presented")]
    internal static partial IGenericMessage NoSuchRequest(
        ILogger<InMemoryAuthorizationRequestStore> logger);
}
