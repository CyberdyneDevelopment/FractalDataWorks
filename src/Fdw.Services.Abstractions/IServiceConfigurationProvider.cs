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
// Why this exists: a parent provider stores its typed-body providers keyed by ServiceOptionType and
// only ever reads, saves or deletes through them — it never uses the body's concrete type. Keying that
// registry on the generic interface forced an 83-line forwarding adapter to widen every result, because
// IServiceConfigurationProvider{TConfig} cannot be covariant (Task{T} is invariant). Erasing to this
// interface at the registry boundary performs the same widening the dictionary already did, without the
// adapter and without needing the body's concrete type at compile time.
//
// Why this is NOT a base of the generic interface: they are two views of one provider over different
// types, not a specialisation. Keeping them separate lets a provider declare each one deliberately —
// a provider that serves the registry says so in its base list — and it is what allows both to spell
// the operation Get, since a class satisfies the colliding pair through explicit implementation.
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
    // Why this is on the erased view and not only the typed one: name resolution is the PRIMARY
    // path — a provider resolves by name far more often than by id — and a holder of the erased
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
// Why TConfig is not covariant (out): Task{TResult} is invariant in TResult, so covariance on an
// async-returning interface is impossible in C#. Use concrete types as TConfig throughout, not
// interfaces — the erased view above is how a caller holds a provider without naming its type.
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
