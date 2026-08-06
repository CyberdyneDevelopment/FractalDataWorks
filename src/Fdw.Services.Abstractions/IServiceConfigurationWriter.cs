using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Write view of a service configuration provider.
/// Provides Save and Delete operations.
/// </summary>
/// <typeparam name="TConfig">The configuration type.</typeparam>
// Why: T is invariant — Task{TResult} is invariant in TResult; covariance on an async-returning
// interface is impossible in C#.
public interface IServiceConfigurationWriter<TConfig>
    where TConfig : class, IGenericConfiguration
{
    /// <summary>
    /// Persists a configuration record (INSERT for new, UPDATE for existing by Id) via
    /// the underlying IConfigurationGateway.
    /// </summary>
    Task<IGenericResult<TConfig>> Save(TConfig record, CancellationToken ct = default);

    /// <summary>
    /// Deletes (soft-deletes) a configuration record by Id via the underlying
    /// IConfigurationGateway. No-op if id is empty.
    /// </summary>
    Task<IGenericResult> Delete(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Deletes (soft-deletes) a configuration record by name via the underlying
    /// IConfigurationGateway. Not all providers support name-based deletion.
    /// </summary>
    Task<IGenericResult> Delete(string name, CancellationToken ct = default);
}
