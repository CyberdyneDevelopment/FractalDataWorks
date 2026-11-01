using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>MessageLogging for the route a database connection gives a command: serial SQL or its bulk mode (FDW-1152).</summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class DatabaseCommandLog
{
    /// <summary>Logs a command rerouted to the connection's bulk mode because it has more rows than an insert runs serially: the connection's MaxSerialInsert, or fewer where one request cannot carry that many rows of the container's columns.</summary>
    // Why Information: which route a write took is what an operator asks first when a load is slow or a bulk copy misbehaves.
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Command '{commandType}' on connection '{connectionName}' has more rows than the {maxSerialInsert} an insert runs serially and takes the bulk route{inTransaction}")]
    public static partial IGenericMessage BulkRouteChosen(ILogger logger, string commandType, string connectionName, int maxSerialInsert, string inTransaction);

    /// <summary>Logs a command that wants the bulk route on a connection that cannot give it.</summary>
    // Why a coded result and not an exception: the connection returns it, so the caller reads which command wanted bulk and where.
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "Command '{commandType}' wants the bulk route but connection '{connectionName}' cannot give it: {reason}")]
    public static partial IGenericMessage BulkRouteUnavailable(ILogger logger, string commandType, string connectionName, string reason);

    /// <summary>Logs a connection whose configuration does not state how many rows an insert may run serially.</summary>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "Connection '{connectionName}' does not state MaxSerialInsert, the most rows an insert runs serially; state a positive number in the connection's configuration")]
    public static partial IGenericMessage MaxSerialInsertNotStated(ILogger logger, string connectionName);
}
