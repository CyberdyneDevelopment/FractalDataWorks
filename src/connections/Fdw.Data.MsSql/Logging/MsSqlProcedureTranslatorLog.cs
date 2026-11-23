using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql.Logging;

/// <summary>
/// Message logging for the translator of <c>ExecuteProcedureCommand</c> (FDW-6682).
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlProcedureTranslatorLog
{
    /// <summary>Logs that a procedure parameter holds a value that is not a scalar, so the call was refused and nothing was sent.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="procedureName">The procedure the call names.</param>
    /// <param name="parameterName">The parameter whose value is not a scalar.</param>
    /// <param name="valueType">The type of the value.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> the refused call carries.</returns>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error,
        Message = "[MsSqlProcedureTranslator] Parameter '{parameterName}' of procedure '{procedureName}' holds a {valueType}, which is not a scalar (text, boolean, number, Guid, date or null), so the call was refused")]
    public static partial IGenericMessage ParameterNotScalar(ILogger logger, string procedureName, string parameterName, string valueType);

    /// <summary>Logs that a procedure parameter's name is not an identifier, so the call was refused and nothing was sent.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="procedureName">The procedure the call names.</param>
    /// <param name="parameterName">The parameter name that is not an identifier.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> the refused call carries.</returns>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error,
        Message = "[MsSqlProcedureTranslator] Parameter name '{parameterName}' of procedure '{procedureName}' is not an identifier (letters, digits and underscores, not starting with a digit), so the call was refused")]
    public static partial IGenericMessage ParameterNameNotIdentifier(ILogger logger, string procedureName, string parameterName);

    /// <summary>Logs that the container a procedure call runs against has no database address, so the call was refused.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The container.</param>
    /// <param name="pathType">The type of the container's path.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> the refused call carries.</returns>
    [MessageLogging(EventId = 60003, Level = LogLevel.Error,
        Message = "[MsSqlProcedureTranslator] Container '{containerName}' has a {pathType} path, not a database path, so the procedure call was refused")]
    public static partial IGenericMessage ContainerPathNotDatabase(ILogger logger, string containerName, string pathType);

    /// <summary>Logs the procedure call a translator built (trace).</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="procedureName">The procedure.</param>
    /// <param name="parameterCount">The number of bound parameters.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 20003, Level = LogLevel.Trace,
        Message = "[MsSqlProcedureTranslator] Built the call of procedure '{procedureName}' with {parameterCount} bound parameter(s)")]
    public static partial IGenericMessage CallBuilt(ILogger logger, string procedureName, int parameterCount);
}
