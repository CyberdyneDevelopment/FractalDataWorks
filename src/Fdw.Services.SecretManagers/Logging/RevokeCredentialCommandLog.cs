using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.RevokeCredentialCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class RevokeCredentialCommandLog
{
    [MessageLogging(
        EventId = 12006,
        Level = LogLevel.Trace,
        Message = "RevokeCredentialCommand constructed for credential '{credentialId}' of type '{credentialType}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        System.Guid credentialId,
        string credentialType);

    // Why: reuses the FDW canonical RequiredValueMissing number (20000) — see
    // SecretManagerCommandBaseLog.RequiredValueMissing's remark.
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "RevokeCredentialCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
