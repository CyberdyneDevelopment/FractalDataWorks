using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlGateway;

/// <summary>
/// MessageLogging for the ETL gateway (FDW-1479): why a script is refused, and how a run went.
/// EventId range: 20980-20989.
/// </summary>
[MessageLoggingTypeCode("ETLGATEWAY")]
public static partial class EtlGatewayLog
{
    /// <summary>Logs a scripted command that is unsound before any store is reached.</summary>
    [MessageLogging(EventId = 20980, Level = LogLevel.Warning, Message = "The {commandType} script for data store '{dataStore}' is refused: {problem}")]
    public static partial IGenericMessage ScriptedCommandInvalid(ILogger logger, string dataStore, string commandType, string problem);

    /// <summary>Logs a script for a store the host declares.</summary>
    [MessageLogging(EventId = 20981, Level = LogLevel.Error, Message = "Data store '{dataStore}' is declared by the host, which does not take scripts: the {commandType} script is refused")]
    public static partial IGenericMessage HostStoreRefused(ILogger logger, string dataStore, string commandType);

    /// <summary>Logs a script for a store whose connection has no script extension on this host.</summary>
    [MessageLogging(EventId = 20982, Level = LogLevel.Error, Message = "Data store '{dataStore}' does not take scripts on this host: its connection has no script extension here, so the {commandType} script is refused")]
    public static partial IGenericMessage NotSupported(ILogger logger, string dataStore, string commandType);

    /// <summary>Logs a script run by a caller without the data engineer role.</summary>
    [MessageLogging(EventId = 20983, Level = LogLevel.Error, Message = "The {commandType} script for data store '{dataStore}' is refused: only the {role} role may run scripts")]
    public static partial IGenericMessage RoleRequired(ILogger logger, string dataStore, string commandType, string role);

    /// <summary>Logs a script about to run.</summary>
    [MessageLogging(EventId = 20984, Level = LogLevel.Debug, Message = "Running the {commandType} script on data store '{dataStore}'")]
    public static partial IGenericMessage Running(ILogger logger, string commandType, string dataStore);

    /// <summary>Logs a script that ran.</summary>
    [MessageLogging(EventId = 20985, Level = LogLevel.Debug, Message = "The {commandType} script on data store '{dataStore}' completed in {elapsedMs}ms")]
    public static partial IGenericMessage Completed(ILogger logger, string commandType, string dataStore, double elapsedMs);

    /// <summary>Logs a script that failed.</summary>
    [MessageLogging(EventId = 20986, Level = LogLevel.Error, Message = "The {commandType} script on data store '{dataStore}' failed: {reason}")]
    public static partial IGenericMessage Failed(ILogger logger, string commandType, string dataStore, string? reason);

    /// <summary>Logs a data store that cannot be resolved to a connection.</summary>
    [MessageLogging(EventId = 20987, Level = LogLevel.Error, Message = "Data store '{dataStore}' cannot be run a script on: {reason}")]
    public static partial IGenericMessage StoreNotResolved(ILogger logger, string dataStore, string reason);
}
