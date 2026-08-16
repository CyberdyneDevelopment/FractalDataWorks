using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Data.MsSql.Translators.MsSqlDataCommandTranslatorBase"/>
/// SQL-command and parameter construction helpers shared by every MsSql translator.
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlDataCommandTranslatorBaseLog
{
    /// <summary>
    /// Logs when a parameter value is an <see cref="System.Collections.IEnumerable"/> (not a string)
    /// and is being JSON-serialized for an NVARCHAR(MAX) JSON column instead of passed natively.
    /// </summary>
    [MessageLogging(
        EventId = 12000,
        Level = LogLevel.Debug,
        Message = "Serializing enumerable value for parameter '{parameterName}' to JSON for NVARCHAR(MAX) column")]
    public static partial IGenericMessage SerializingEnumerableParameter(
        ILogger logger,
        string parameterName);
}
