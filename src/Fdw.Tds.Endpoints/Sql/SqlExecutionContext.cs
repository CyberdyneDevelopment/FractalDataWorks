using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Tds.Endpoints.Hosting;
using Fdw.Tds.Endpoints.Sql.Catalog;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// Everything one request runs against: the session, the server it presents, the endpoint resolved for
/// the caller, the response being written, and the request's variables.
/// </summary>
public sealed class SqlExecutionContext
{
    /// <summary>The deepest procedures and dynamic SQL nest, SQL Server's own limit; one level more is error 217.</summary>
    public const int MaximumNestLevel = 32;

    private readonly CatalogCache _catalog;

    /// <summary>Initializes a new instance of the <see cref="SqlExecutionContext"/> class.</summary>
    /// <param name="session">The connection's session.</param>
    /// <param name="server">The SQL Server this endpoint presents itself as.</param>
    /// <param name="endpoint">The endpoint resolved for this request's caller.</param>
    /// <param name="response">The response being written.</param>
    /// <param name="logger">The logger.</param>
    public SqlExecutionContext(TdsSession session, TdsServerIdentity server, TdsEndpointBase endpoint, TdsResponse response, ILogger logger)
        : this(session, server, endpoint, response, logger, new SqlVariables(), new CatalogCache(endpoint), DateTimeOffset.Now, inProcedure: false, nestLevel: 0)
    {
    }

    private SqlExecutionContext(
        TdsSession session,
        TdsServerIdentity server,
        TdsEndpointBase endpoint,
        TdsResponse response,
        ILogger logger,
        SqlVariables variables,
        CatalogCache catalog,
        DateTimeOffset requestTime,
        bool inProcedure,
        int nestLevel)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Server = server ?? throw new ArgumentNullException(nameof(server));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Response = response ?? throw new ArgumentNullException(nameof(response));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Variables = variables;
        _catalog = catalog;
        RequestTime = requestTime;
        InProcedure = inProcedure;
        NestLevel = nestLevel;
    }

    /// <summary>Gets the connection's session.</summary>
    public TdsSession Session { get; }

    /// <summary>Gets the SQL Server this endpoint presents itself as.</summary>
    public TdsServerIdentity Server { get; }

    /// <summary>Gets the endpoint resolved for this request's caller.</summary>
    public TdsEndpointBase Endpoint { get; }

    /// <summary>Gets the response being written.</summary>
    public TdsResponse Response { get; }

    /// <summary>Gets the logger.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the variables statements in this context see.</summary>
    public SqlVariables Variables { get; }

    /// <summary>Gets the moment the request began; GETDATE and its kin answer it throughout the request.</summary>
    public DateTimeOffset RequestTime { get; }

    /// <summary>Gets or sets a value indicating whether statements run inside a procedure, ending in DONEINPROC.</summary>
    public bool InProcedure { get; set; }

    /// <summary>Gets how deep in procedures and dynamic SQL these statements run (<c>@@NESTLEVEL</c>); zero for the request's own batch.</summary>
    public int NestLevel { get; }

    /// <summary>Gets the catalog, once <see cref="LoadCatalog"/> has read it.</summary>
    public CatalogSnapshot? LoadedCatalog => _catalog.Loaded;

    /// <summary>Reads the catalog the first time a statement needs it, and keeps it for the request.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The catalog, or the failure reading it.</returns>
    public Task<IGenericResult<CatalogSnapshot>> LoadCatalog(CancellationToken cancellationToken = default) => _catalog.Load(cancellationToken);

    /// <summary>
    /// A context one level deeper, for statements the request runs on their own variables: sp_executesql, EXEC('text').
    /// </summary>
    /// <remarks>
    /// Refused past <see cref="MaximumNestLevel"/> with error 217, as SQL Server refuses it. Without the limit a batch
    /// that runs itself recurses until the process runs out of stack, a failure no handler can catch and one that
    /// takes every other connection down with it.
    /// </remarks>
    /// <param name="variables">The variables those statements see.</param>
    /// <param name="inProcedure">Whether they end in DONEINPROC.</param>
    /// <param name="line">The line of the call that nests them.</param>
    /// <returns>The context, sharing this one's session, response and catalog; or error 217.</returns>
    public IGenericResult<SqlExecutionContext> Nested(SqlVariables variables, bool inProcedure, int line)
        => NestLevel >= MaximumNestLevel
            ? SqlRefusal.Refuse<SqlExecutionContext>(SqlServerErrors.NestingTooDeep(MaximumNestLevel, line), Logger)
            : GenericResult<SqlExecutionContext>.Success(
                new SqlExecutionContext(Session, Server, Endpoint, Response, Logger, variables, _catalog, RequestTime, inProcedure, NestLevel + 1));
}
