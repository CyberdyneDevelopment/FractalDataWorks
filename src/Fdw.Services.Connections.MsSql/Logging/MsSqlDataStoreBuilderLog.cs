using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql.Logging;

/// <summary>
/// MessageLogging for the MsSql data-store builder (container/field construction diagnostics).
/// EventId range: 8776-8779
/// </summary>
[MessageLoggingTypeCode("MSSQL")]
public static partial class MsSqlDataStoreBuilderLog
{
    /// <summary>
    /// Logs which MsSql container subtype was chosen for a container at the transport boundary.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The name of the container being built.</param>
    /// <param name="subtype">The chosen container subtype (e.g. MsSqlTable, MsSqlView).</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Debug,
        Message = "MsSql container subtype chosen for '{containerName}': {subtype}")]
    public static partial IGenericMessage ContainerSubtypeChosen(ILogger logger, string containerName, string subtype);

    /// <summary>
    /// Logs how many typed fields were resolved while building a container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The name of the container being built.</param>
    /// <param name="fieldCount">The number of MsSql fields resolved for the container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Debug,
        Message = "MsSql container '{containerName}': {fieldCount} fields resolved")]
    public static partial IGenericMessage FieldsResolved(ILogger logger, string containerName, int fieldCount);

    /// <summary>
    /// Logs that the SQL native type for a field was resolved from its DataType.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="fieldName">The name of the field being built.</param>
    /// <param name="nativeType">The resolved SQL native type name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Debug,
        Message = "MsSql field '{fieldName}': native type resolved to '{nativeType}'")]
    public static partial IGenericMessage FieldNativeTypeResolved(ILogger logger, string fieldName, string nativeType);
}
