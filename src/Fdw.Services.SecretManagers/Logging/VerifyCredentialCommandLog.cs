using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.VerifyCredentialCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class VerifyCredentialCommandLog
{
    // Why: never logs CandidateValue — only the non-sensitive user/credential-type identifiers.
    [MessageLogging(
        EventId = 12008,
        Level = LogLevel.Trace,
        Message = "VerifyCredentialCommand constructed for user '{userId}' credential type '{credentialType}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        System.Guid userId,
        string credentialType);

    // Why: reuses the FDW canonical RequiredValueMissing number (20000) — see
    // SecretManagerCommandBaseLog.RequiredValueMissing's remark.
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "VerifyCredentialCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
