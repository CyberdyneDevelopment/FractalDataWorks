using System;
using System.Collections.Generic;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The names of the connections one request may show, read once (see <see cref="ConnectionNames"/>).
/// </summary>
internal sealed class ConnectionNameLookup
{
    private readonly bool _mayReadConnections;
    private readonly IReadOnlyDictionary<Guid, string> _names;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionNameLookup"/> class.</summary>
    /// <param name="mayReadConnections">Whether the caller passes the connections read policy.</param>
    /// <param name="names">The name of each connection the caller may see, by the connection's id.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionNameLookup(bool mayReadConnections, IReadOnlyDictionary<Guid, string> names, ILogger logger)
    {
        _mayReadConnections = mayReadConnections;
        _names = names ?? throw new ArgumentNullException(nameof(names));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the name of a connection, or null when this caller may not see it.</summary>
    /// <param name="connectionId">The connection the data store names.</param>
    /// <param name="dataStoreName">The data store, for the log.</param>
    /// <returns>The name; null when the caller may not read connections or the row is not visible to them.</returns>
    public string? Of(Guid connectionId, string dataStoreName)
    {
        if (!_mayReadConnections)
        {
            DataStoreEndpointLog.ConnectionNameLeftOut(_logger, connectionId, dataStoreName, "the caller may not read connections");
            return null;
        }

        if (_names.TryGetValue(connectionId, out var name))
            return name;

        DataStoreEndpointLog.ConnectionNameLeftOut(_logger, connectionId, dataStoreName, "the connection is not visible to the caller");
        return null;
    }
}
