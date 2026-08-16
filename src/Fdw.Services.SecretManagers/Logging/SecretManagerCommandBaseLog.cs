using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.SecretManagerCommandBase"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class SecretManagerCommandBaseLog
{
    [MessageLogging(
        EventId = 12009,
        Level = LogLevel.Trace,
        Message = "SecretManagerCommandBase validating command '{commandType}'")]
    public static partial IGenericMessage Validating(
        ILogger logger,
        string commandType);

    [MessageLogging(
        EventId = 12010,
        Level = LogLevel.Debug,
        Message = "SecretManagerCommandBase: command '{commandType}' passed validation")]
    public static partial IGenericMessage ValidationPassed(
        ILogger logger,
        string commandType);

    // Why: 20000 is the FDW canonical RequiredValueMissing number (RESULTCODE-CATALOG.md), reused
    // here (and by every other SecretManagers command's constructor guard) for the same semantic
    // condition — a required constructor argument was null/empty — differentiated by {parameterName}.
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "SecretManagerCommandBase: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
