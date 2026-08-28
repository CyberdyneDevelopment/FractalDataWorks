using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.StoreCredentialCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class StoreCredentialCommandLog
{
    [MessageLogging(
        EventId = 12007,
        Level = LogLevel.Trace,
        Message = "StoreCredentialCommand constructed for user '{userId}' credential type '{credentialType}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        System.Guid userId,
        string credentialType);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "StoreCredentialCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
