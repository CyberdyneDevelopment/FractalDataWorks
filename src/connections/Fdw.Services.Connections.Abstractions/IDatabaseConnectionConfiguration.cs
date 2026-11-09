namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The configuration of a database connection (FDW-1152): what only a database connection states about how it writes.
/// </summary>
/// <remarks>
/// Why a separate interface and not a member of every connection configuration: only a database connection can reroute a
/// command to a bulk mode, so only its configuration states when. An HTTP, file or any other connection's configuration
/// never carries it.
/// </remarks>
public interface IDatabaseConnectionConfiguration : IConnectionImplementationConfiguration
{
    /// <summary>
    /// Gets or sets the most rows an insert runs serially, as ordinary SQL; an insert with more rows is rerouted to the
    /// connection's bulk mode (SqlBulkCopy on SQL Server, COPY on PostgreSQL), inside the unit of work when there is one.
    /// </summary>
    /// <remarks>
    /// Stated in the connection's configuration and never defaulted: zero means it was not stated, and a connection whose
    /// configuration leaves it so is refused when it is created, naming the connection.
    /// </remarks>
    int MaxSerialInsert { get; set; }

    /// <summary>
    /// Gets or sets the most values a key list carries as parameters in a statement of this connection before the key list goes to a temporary table the statement
    /// joins on, or <see langword="null"/> when the connection states none (FDW-4267).
    /// </summary>
    /// <remarks>
    /// Null means the connection states none: the default declared on its connection type's service type option applies. Nothing is assumed here or in the column.
    /// </remarks>
    int? MaxParametersBeforeTempTable { get; set; }
}
