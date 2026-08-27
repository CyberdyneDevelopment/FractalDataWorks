using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Holds framework-internal connections that were built before the configuration system existed —
/// ConfigurationDb itself, chiefly — and hands them back by name.
/// </summary>
/// <remarks>
/// It is not an <see cref="IConnectionProvider"/>. A platform provider resolves a configuration and
/// dispatches on its ServiceOptionType to a registered factory; this one is a name-to-instance registry
/// whose entries are handed to it pre-built. Declaring it as a connection provider obliged it to answer
/// factory registration, configuration registration and build-from-configuration calls that have no
/// meaning for a fixed registry, which it did by returning failures and no-ops — a contract it appeared
/// to satisfy and did not.
/// </remarks>
public interface IServiceConnectionProvider
{
    /// <summary>Registers a pre-built connection under a logical name.</summary>
    /// <param name="name">The logical name, matched case-insensitively.</param>
    /// <param name="connection">The already-created connection.</param>
    void Register(string name, IGenericConnection connection);

    /// <summary>Gets a registered connection by name.</summary>
    /// <param name="name">The logical name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The connection, or a structured failure when the name is not registered.</returns>
    Task<IGenericResult<IGenericConnection>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a registered connection by its durable id.</summary>
    /// <param name="id">The connection's id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The connection, or a structured failure when the id is not registered.</returns>
    Task<IGenericResult<IGenericConnection>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets every registered connection.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The registered connections.</returns>
    Task<IGenericResult<IReadOnlyList<IGenericConnection>>> Get(CancellationToken cancellationToken = default);
}
