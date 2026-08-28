using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Abstractions;

/// <summary>
/// The type-erased surface of a configuration provider: the three operations a parent provider
/// performs on a typed-body provider it holds by discriminator.
/// </summary>
public interface IServiceConfigurationProvider
{
    /// <summary>Reads a configuration record by its durable id.</summary>
    /// <param name="id">The durable identifier of the record.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The record, widened to the configuration abstraction.</returns>
    Task<IGenericResult<IGenericConfiguration>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Gets a configuration by name.</summary>
    /// <param name="name">The configuration's name.</param>
    /// <param name="ct">Cancels the lookup.</param>
    /// <returns>The configuration, or a structured failure.</returns>
    Task<IGenericResult<IGenericConfiguration>> Get(string name, CancellationToken ct = default);

    /// <summary>Saves a configuration record.</summary>
    /// <param name="record">The record to save.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>Success, or a failure naming why the record could not be saved.</returns>
    Task<IGenericResult> Save(IGenericConfiguration record, CancellationToken ct = default);

    /// <summary>Deletes a configuration record by its durable id.</summary>
    /// <param name="id">The durable identifier of the record.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>Success, or a failure naming why the record could not be deleted.</returns>
    Task<IGenericResult> Delete(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Provides read and write access to service configurations of one concrete type.
/// </summary>
/// <typeparam name="TConfig">The configuration type.</typeparam>
public interface IServiceConfigurationProvider<TConfig>
    where TConfig : IGenericConfiguration
{
    /// <summary>Gets a configuration by ID.</summary>
    Task<IGenericResult<TConfig>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Gets a configuration by name.</summary>
    Task<IGenericResult<TConfig>> Get(string name, CancellationToken ct = default);

    /// <summary>Gets all configurations.</summary>
    Task<IGenericResult<IReadOnlyList<TConfig>>> Get(CancellationToken ct = default);

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
