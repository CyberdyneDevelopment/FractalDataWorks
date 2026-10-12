using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads the names of the connections data stores are reached through, for a caller who may see connections.
/// </summary>
/// <remarks>
/// Connections are for administrators to see. A data store response names its connection only when the
/// caller passes the policy the connections route itself applies to a read, and the connection is visible
/// to that caller under row-level security. Otherwise the name is left out (null), which the response
/// declares. A connection that could not be READ is a fault and fails: it is never taken for "not visible".
/// <para>
/// The names of a whole page are read in ONE query: the connection domain table, by the ids the page's
/// stores name, asked of the connection domain's own provider. A name is another domain's, so it is never
/// joined in from the store's table.
/// </para>
/// </remarks>
internal sealed class ConnectionNames
{
    // The policy every connections read route applies: its resource name and ":read".
    private const string ReadPolicy = "connections:read";

    private readonly IDomainRecordProvider<DomainConfiguration> _connections;
    private readonly IAuthorizationService _authorization;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionNames"/> class.</summary>
    /// <param name="connections">Reads the connection domain records (the connection provider).</param>
    /// <param name="authorization">Asks whether the caller passes the connections read policy.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionNames(IDomainRecordProvider<DomainConfiguration> connections, IAuthorizationService authorization, ILogger? logger)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Reads the names of the connections a page of data stores names.</summary>
    /// <param name="connectionIds">The connections the stores name; duplicates are asked once.</param>
    /// <param name="caller">The signed-in caller.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The lookup, which names each connection the caller may see and leaves out the rest; a failure when the
    /// read failed. The policy is asked ONCE, here, for the whole page: a caller who may not read connections
    /// gets a lookup that names none, and no connection is read at all.
    /// </returns>
    public async Task<IGenericResult<ConnectionNameLookup>> Read(
        IReadOnlyCollection<Guid> connectionIds, ClaimsPrincipal caller, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connectionIds);

        var allowed = await _authorization.AuthorizeAsync(caller, null, ReadPolicy).ConfigureAwait(false);
        if (!allowed.Succeeded)
            return GenericResult<ConnectionNameLookup>.Success(
                new ConnectionNameLookup(false, new Dictionary<Guid, string>(), _logger));

        var found = await _connections.Get(connectionIds, ct).ConfigureAwait(false);
        if (!found.IsSuccess)
            return found.ToNewResult<ConnectionNameLookup>();

        return GenericResult<ConnectionNameLookup>.Success(
            new ConnectionNameLookup(true, found.Value!.ToDictionary(connection => connection.Id, connection => connection.Name), _logger));
    }
}
