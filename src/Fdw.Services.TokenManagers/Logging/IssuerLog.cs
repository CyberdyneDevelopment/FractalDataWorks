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
    // Why Information: this is the record that authentication succeeded, and an auditor wants the
    // assurance level alongside it without tracing being on.
    [MessageLogging(EventId = 91180, Level = LogLevel.Information,
        Message = "Issued a token for audience '{audience}' naming principal {principalId} at assurance '{acr}'")]
    internal static partial IGenericMessage Issued(
        ILogger<JwtTokenIssuer> logger, string audience, Guid principalId, string acr);

    /// <summary>No request was supplied.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91181, Level = LogLevel.Error,
        Message = "A request must be supplied to issue a token")]
    internal static partial IGenericMessage RequestMissing(ILogger<JwtTokenIssuer> logger);

    /// <summary>The request named no audience.</summary>
    /// <param name="logger">The logger.</param>
    // Why refused rather than defaulted: a token with no audience, or a wildcard one, is accepted
    // by every resource server that receives it. There is no safe default.
    [MessageLogging(EventId = 91182, Level = LogLevel.Error,
        Message = "A token cannot be issued without an audience")]
    internal static partial IGenericMessage AudienceMissing(ILogger<JwtTokenIssuer> logger);
}
