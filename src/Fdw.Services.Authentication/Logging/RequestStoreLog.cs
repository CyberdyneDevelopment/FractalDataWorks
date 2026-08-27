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
    // Why Error: state comes from the CSPRNG, so a collision means either it is not random or
    // something is replaying. Neither is survivable quietly.
    [MessageLogging(EventId = 91222, Level = LogLevel.Error,
        Message = "A state value was reused; refusing to overwrite the pending request")]
    internal static partial IGenericMessage StateReused(
        ILogger<InMemoryAuthorizationRequestStore> logger);

    /// <summary>Nothing was pending under the state presented.</summary>
    /// <param name="logger">The logger.</param>
    // Why Warning: unknown, already consumed, or from an instance that no longer holds it. All
    // refused, and the last is the load-balancer case worth noticing.
    [MessageLogging(EventId = 91223, Level = LogLevel.Warning,
        Message = "No pending authorization request matches the state presented")]
    internal static partial IGenericMessage NoSuchRequest(
        ILogger<InMemoryAuthorizationRequestStore> logger);
}
