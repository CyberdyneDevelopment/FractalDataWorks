using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.GetCertificateManagerCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class GetCertificateManagerCommandLog
{
    [MessageLogging(
        EventId = 12004,
        Level = LogLevel.Trace,
        Message = "GetCertificateManagerCommand constructed for container '{container}' certificate '{certificateName}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container,
        string certificateName);

    // Why: reuses the FDW canonical RequiredValueMissing number (20000) — see
    // SecretManagerCommandBaseLog.RequiredValueMissing's remark.
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "GetCertificateManagerCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
