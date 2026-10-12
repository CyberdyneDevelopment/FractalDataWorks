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

    /// <summary>
    /// Logs when a null value whose property is declared <c>byte[]</c> is bound as VARBINARY, so SQL
    /// Server does not receive an untyped NULL as NVARCHAR for a binary column (FDW-918).
    /// </summary>
    [MessageLogging(
        EventId = 12001,
        Level = LogLevel.Debug,
        Message = "Binding null value for parameter '{parameterName}' as VARBINARY from its declared byte[] type")]
    public static partial IGenericMessage BindingNullBinaryParameter(
        ILogger logger,
        string parameterName);
}
