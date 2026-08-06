using System;
using System.Collections.Generic;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Limits;

/// <summary>
/// Resolves the effective set of connection limits for a named connection at runtime.
///
/// Walks the Connection → DataStore → DataSet → Step override chain and returns
/// the strictest value per limit kind (lower cap always wins; relaxation is rejected
/// at save time so it cannot arrive here).
///
/// The resolver is the single lookup point for limits; it abstracts the configuration
/// store (in-memory, DB-backed, or test double) from the enforcement layer.
/// </summary>
public interface IConnectionLimitResolver
{
    /// <summary>
    /// Returns the effective active limits for the specified connection.
    /// Returns an empty list when no limits are configured (enforcement is a no-op).
    /// </summary>
    /// <param name="connectionName">The logical name of the connection (matches IDataCommand.ConnectionName).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IGenericResult<IReadOnlyList<ConnectionLimitConfiguration>> Resolve(
        string connectionName,
        CancellationToken cancellationToken = default);
}
