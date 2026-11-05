using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Documents.Records.Logging;

/// <summary>
/// MessageLogging for FDW's document record store (FDW-3673): what it sends to an organization's DocumentRecord connection and what it refuses
/// before sending. EventId range: 22200-22209.
/// </summary>
[MessageLoggingTypeCode("DOCUMENTS")]
public static partial class DocumentRecordStoreLog
{
    /// <summary>Logs a command sent to the organization's connection.</summary>
    [MessageLogging(EventId = 22200, Level = LogLevel.Debug, Message = "Sending the '{command}' command for organization '{organizationId}' to its DocumentRecord connection")]
    public static partial IGenericMessage CommandSent(ILogger logger, string command, Guid organizationId);

    /// <summary>Logs a command refused before it was sent.</summary>
    [MessageLogging(EventId = 22201, Level = LogLevel.Warning, Message = "The '{command}' command for organization '{organizationId}' was refused before it was sent: {reason}")]
    public static partial IGenericMessage CommandRefused(ILogger logger, string command, Guid organizationId, string reason);
}
