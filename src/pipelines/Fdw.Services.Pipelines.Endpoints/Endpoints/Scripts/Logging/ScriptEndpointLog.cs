using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints.Logging;

/// <summary>
/// MessageLogging for the stored script endpoints (FDW-1442): checking a draft, and reading a stored script.
/// EventId range: 20970-20971 and 20975-20979 (20972-20974 are Fdw.Services.Etl.Scripts.ScriptLog).
/// </summary>
[MessageLoggingTypeCode("SCRIPTS")]
public static partial class ScriptEndpointLog
{
    /// <summary>Logs a script draft being checked without being saved.</summary>
    [MessageLogging(EventId = 20970, Level = LogLevel.Information, Message = "Checking draft script '{script}' without saving it")]
    public static partial IGenericMessage DraftCheckStarted(ILogger logger, string script);

    /// <summary>Logs the outcome of checking a script draft.</summary>
    [MessageLogging(EventId = 20971, Level = LogLevel.Information, Message = "Checked draft script '{script}': {problemCount} problem(s)")]
    public static partial IGenericMessage DraftCheckFinished(ILogger logger, string script, int problemCount);

    /// <summary>Logs a stored script that was saved and cannot be read back.</summary>
    [MessageLogging(EventId = 20975, Level = LogLevel.Error, Message = "Script '{script}' was saved and cannot be read back")]
    public static partial IGenericMessage SavedButUnreadable(ILogger logger, string script);

    /// <summary>Logs a stored script of an implementation this endpoint does not read.</summary>
    [MessageLogging(EventId = 20976, Level = LogLevel.Error, Message = "Script '{script}' is a {implementation} script, not a SQL script")]
    public static partial IGenericMessage NotASqlScript(ILogger logger, string script, string implementation);
}
