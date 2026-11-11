using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="DataCommandBase"/> construction — the shared base every data
/// command (Query, Insert, Update, Delete, ...) passes through.
/// </summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class DataCommandBaseLog
{
    /// <summary>Traces a data command instance being constructed.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "[DataCommandBase] Created command {commandId} of type '{commandType}' in category '{category}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, Guid commandId, string commandType, string category);
}
