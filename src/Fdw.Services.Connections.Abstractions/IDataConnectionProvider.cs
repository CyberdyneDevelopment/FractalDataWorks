using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for providers that retrieve data connections.
/// </summary>
public interface IDataConnectionProvider
{
    /// <summary>Gets a data connection by configuration name.</summary>
    Task<IGenericResult<IDataConnection>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a data connection by configuration ID.</summary>
    Task<IGenericResult<IDataConnection>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a data connection built from the supplied connection configuration —
    /// no name/id lookup is performed.
    /// </summary>
    Task<IGenericResult<IDataConnection>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Gets all registered data connections.</summary>
    Task<IGenericResult<IReadOnlyList<IDataConnection>>> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a data connection by name cast to the specified typed connection interface.
    /// </summary>
    Task<IGenericResult<T>> Get<T>(string name, CancellationToken cancellationToken = default) where T : IDataConnection;
}
