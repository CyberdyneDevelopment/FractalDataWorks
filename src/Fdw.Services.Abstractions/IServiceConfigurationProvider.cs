using System;
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
public interface IServiceConfigurationProvider
{
    /// <summary>Reads a configuration record by its durable id.</summary>
    /// <param name="id">The durable identifier of the record.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The record, widened to the configuration abstraction.</returns>
    Task<IGenericResult<IGenericConfiguration>> GetUntyped(Guid id, CancellationToken ct = default);

    /// <summary>Saves a configuration record.</summary>
    /// <param name="record">The record to save.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>Success, or a failure naming why the record could not be saved.</returns>
    Task<IGenericResult> SaveUntyped(IGenericConfiguration record, CancellationToken ct = default);

    /// <summary>Deletes a configuration record by its durable id.</summary>
    /// <param name="id">The durable identifier of the record.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>Success, or a failure naming why the record could not be deleted.</returns>
    Task<IGenericResult> DeleteUntyped(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Provides full read+write access to service configurations.
/// Composes IServiceConfigurationReader{TConfig} and IServiceConfigurationWriter{TConfig}.
/// </summary>
/// <typeparam name="TConfig">The configuration type.</typeparam>
// Why: TConfig cannot be covariant (out) because Task{T} is invariant.
// Use concrete types as TConfig throughout (not interfaces).
public interface IServiceConfigurationProvider<TConfig>
    : IServiceConfigurationProvider, IServiceConfigurationReader<TConfig>, IServiceConfigurationWriter<TConfig>
    where TConfig : class, IGenericConfiguration
{
}
