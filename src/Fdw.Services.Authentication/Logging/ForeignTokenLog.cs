using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Steps;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for accepting a token another authority issued.
/// </summary>
/// <remarks>
/// EventId range: 91160–91164. The token never appears, at any level. Neither does the subject it
/// names — that identifies a person at an external authority, and the issuer alone is enough to say
/// which trust relationship is involved.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class ForeignTokenLog
{
    /// <summary>A token from a trusted authority passed every check.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that issued it.</param>
    [MessageLogging(EventId = 91160, Level = LogLevel.Trace,
        Message = "A token issued by '{issuer}' was accepted")]
    internal static partial IGenericMessage Accepted(ILogger<ForeignTokenStep> logger, string issuer);

    /// <summary>No token was presented for a step that requires one.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority the flow expected one from.</param>
    [MessageLogging(EventId = 91161, Level = LogLevel.Warning,
        Message = "No token was presented for the flow expecting one issued by '{issuer}'")]
    internal static partial IGenericMessage NoTokenPresented(
        ILogger<ForeignTokenStep> logger, string issuer);

    /// <summary>A presented token failed validation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority it claimed.</param>
    /// <param name="failure">The kind of check that failed.</param>
    // Why Warning: a bad token is what this step exists to reject. Expired, wrong audience, forged —
    // all handled, none a defect here.
    [MessageLogging(EventId = 91162, Level = LogLevel.Warning,
        Message = "A token claiming issuer '{issuer}' was rejected: {failure}")]
    internal static partial IGenericMessage Rejected(
        ILogger<ForeignTokenStep> logger, string issuer, string failure);

    /// <summary>A valid token carried no subject.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="issuer">The authority that issued it.</param>
    // Why Error: a token that verifies and names nobody means the provider is misconfigured — most
    // often a scope that omits the subject claim. Nothing downstream can proceed.
    [MessageLogging(EventId = 91163, Level = LogLevel.Error,
        Message = "A token from '{issuer}' verified but carried no subject claim")]
    internal static partial IGenericMessage NoSubjectClaim(
        ILogger<ForeignTokenStep> logger, string issuer);
}
