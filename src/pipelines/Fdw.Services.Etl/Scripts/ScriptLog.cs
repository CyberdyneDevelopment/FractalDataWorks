using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Scripts;

/// <summary>
/// MessageLogging for running a stored or draft SQL script (FDW-1442).
/// EventId range: 20972-20974.
/// </summary>
[MessageLoggingTypeCode("SCRIPTS")]
public static partial class ScriptLog
{
    /// <summary>Logs a run of a script that is about to go to the data gateway.</summary>
    [MessageLogging(EventId = 20972, Level = LogLevel.Information, Message = "Running {kind} script '{script}' on data store '{dataStore}' with {parameterCount} parameter(s)")]
    public static partial IGenericMessage RunStarted(ILogger logger, string script, string kind, string dataStore, int parameterCount);

    /// <summary>Logs a run that finished.</summary>
    [MessageLogging(EventId = 20973, Level = LogLevel.Information, Message = "Ran {kind} script '{script}' on data store '{dataStore}': {rows} row(s)")]
    public static partial IGenericMessage RunFinished(ILogger logger, string script, string kind, string dataStore, int rows);

    /// <summary>Logs a run the data gateway refused or that failed.</summary>
    [MessageLogging(EventId = 20974, Level = LogLevel.Error, Message = "Script '{script}' failed on data store '{dataStore}': {reason}")]
    public static partial IGenericMessage RunFailed(ILogger logger, string script, string dataStore, string? reason);
}
