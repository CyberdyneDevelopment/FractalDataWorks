using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.PostgreSql.Logging;

/// <summary>
/// Message logging for the PostgreSQL containers (FDW-6387). EventId range: 31001.
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlContainerLog
{
    /// <summary>
    /// Logs that the PostgreSqlTable container type option is not registered in ContainerTypes, so a table container is not built.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerName">The name of the table container that was to be built.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Error,
        Message = "Container type 'PostgreSqlTable' is not registered in ContainerTypes, so table container '{containerName}' cannot be built")]
    public static partial IGenericMessage TableContainerTypeNotRegistered(ILogger logger, string containerName);
}
