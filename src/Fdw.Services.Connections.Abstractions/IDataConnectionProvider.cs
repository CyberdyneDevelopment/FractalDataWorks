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
    // Why: callers that already hold a resolved (composed-header) configuration — e.g. a
    // DataVault that resolved its connection configuration once at Initialize in system
    // context — must not re-resolve by name at request time, where row-level security
    // could filter the lookup for non-privileged callers.
    Task<IGenericResult<IDataConnection>> Get(IGenericConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Gets all registered data connections.</summary>
    Task<IGenericResult<IReadOnlyList<IDataConnection>>> Get(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a data connection by name cast to the specified typed connection interface.
    /// </summary>
    // Why: Typed Get is retained for callers that need connection-type-specific APIs
    // (e.g., ISqlConnection). The type constraint keeps connection type invisible above
    // the connection layer — callers reference an interface, never a concrete class.
    Task<IGenericResult<T>> Get<T>(string name, CancellationToken cancellationToken = default) where T : IDataConnection;
}
