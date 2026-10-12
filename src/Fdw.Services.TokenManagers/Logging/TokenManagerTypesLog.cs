using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging for <see cref="TokenManagerTypes"/>'s registration.
/// </summary>
/// <remarks>
/// EventId range: 91201–91203. This domain is a hand-written three-phase registrant, not a
/// <c>[ServiceTypeCollection]</c> dispatch (see the class's own remarks) — so it does not get the
/// standard invoker's Info-level "running default body" logging for free, and has to log its own
/// registration the way every other registration point in this framework does.
/// </remarks>
[MessageLoggingTypeCode("TOKENMANAGER")]
internal static partial class TokenManagerTypesLog
{
    /// <summary>Issuance and revocation services were registered.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91201, Level = LogLevel.Trace,
        Message = "Registered ITokenIssuer, ISigningCredentialProvider (via JwtIssuanceResolver) and ITokenRevocationStore")]
    internal static partial IGenericMessage Registered(ILogger logger);

    /// <summary>The Jwt implementation configuration provider was connected to the token-manager domain.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="implementation">The Implementation name it was registered under.</param>
    [MessageLogging(EventId = 91202, Level = LogLevel.Information,
        Message = "Connected IJwtTokenManagerConfigurationProvider under Implementation '{implementation}'")]
    internal static partial IGenericMessage ImplementationConnected(ILogger logger, string implementation);

    /// <summary>Connecting the Jwt implementation configuration provider failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="implementation">The Implementation name that could not be registered.</param>
    /// <param name="reason">What the registration reported.</param>
    // Critical: without this, every JwtTokenManager row fails to compose and every login fails at
    // issuance — the exact failure this registration exists to prevent (see the class's own remarks).
    [MessageLogging(EventId = 91203, Level = LogLevel.Critical,
        Message = "Could not connect IJwtTokenManagerConfigurationProvider under Implementation '{implementation}': {reason}")]
    internal static partial IGenericMessage ImplementationConnectFailed(
        ILogger logger, string implementation, string? reason);
}
