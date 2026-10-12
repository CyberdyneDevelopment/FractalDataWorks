using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Tds.Endpoints;

/// <summary>
/// The base a SQL Server endpoint closes over: which schemas and tables a signed-in caller sees, and how
/// a query of one of them is run.
/// </summary>
/// <remarks>
/// <para>
/// Everything that makes a client believe it is talking to SQL Server — the wire protocol, the login, T-SQL,
/// the system catalog, SQL Server's own errors — is this package's. A closure supplies only the relational
/// surface: <see cref="Schemas"/>, <see cref="Tables"/>, <see cref="Table"/> and <see cref="Rows"/>.
/// </para>
/// <para>
/// One instance serves one request. It is resolved from the request's own DI scope with the caller already
/// set on the ambient authentication context, so a closure answers for that caller alone and row-level
/// security applies as it does to an HTTP request. A table the caller may not see is answered as one that
/// does not exist: a failure in the NotFound category, which the client receives as error 208.
/// </para>
/// </remarks>
public abstract class TdsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="TdsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected TdsEndpointBase(ILogger? logger)
    {
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the schemas the caller may read.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The schema names.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<string>>> Schemas(CancellationToken cancellationToken = default);

    /// <summary>Gets the tables of one schema the caller may read.</summary>
    /// <param name="schema">The schema's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tables, each with its columns.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<SqlTable>>> Tables(string schema, CancellationToken cancellationToken = default);

    /// <summary>Gets one table the caller may read.</summary>
    /// <param name="schema">The schema's name.</param>
    /// <param name="name">The table's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The table, or a NotFound-category failure when there is none the caller may see.</returns>
    public abstract Task<IGenericResult<SqlTable>> Table(string schema, string name, CancellationToken cancellationToken = default);

    /// <summary>Runs a query of one table.</summary>
    /// <param name="table">A table <see cref="Table"/> or <see cref="Tables"/> answered.</param>
    /// <param name="command">The query: filter, ordering and paging over the table's own column names.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows, keyed by column name.</returns>
    public abstract Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Rows(
        SqlTable table, QueryCommand<Dictionary<string, object?>> command, CancellationToken cancellationToken = default);
}
