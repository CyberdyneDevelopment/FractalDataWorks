using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// An implementation provider that can write its rows into a unit of work opened by the domain
/// provider that owns them, so a domain record and its implementation commit together.
/// </summary>
/// <typeparam name="TContract">The domain's implementation contract.</typeparam>
/// <remarks>
/// Why this is internal and separate from <see cref="IImplementationConfigurationProvider{TContract}"/>:
/// that interface lives in Fdw.Services.Abstractions, which the data abstractions are built on top of —
/// Fdw.Services.Data.Abstractions references it, and so do the connection and secret-manager
/// abstractions that declare providers of their own. A transaction type therefore cannot appear on it
/// without a reference cycle. Both provider bases live in this assembly, so the contract between them
/// does not have to be public and nothing outside needs to know a transaction is involved. Deriving
/// from the public interface keeps one registry: the domain holds providers by this type and its read
/// paths are unaffected.
/// </remarks>
internal interface IConfigurationWriteParticipant<TContract> : IImplementationConfigurationProvider<TContract>
    where TContract : IImplementationConfiguration
{
    /// <summary>Tells this provider which container its domain rows live in, when the domain provider registers it.</summary>
    /// <param name="domainContainerName">The domain provider's container.</param>
    void AttachDomain(string domainContainerName);

    /// <summary>Writes this implementation's row and its children into the caller's unit of work.</summary>
    /// <param name="record">The implementation configuration to write.</param>
    /// <param name="domain">The domain container the row hangs from.</param>
    /// <param name="domainKeyValues">The domain row's key values; the row's Foreign key to <paramref name="domain"/> is filled from them.</param>
    /// <param name="unit">The unit of work the domain provider opened.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The key values of the version retired and of the row written, or the failure that stopped it.</returns>
    Task<IGenericResult<ConfigurationWriteResult>> Save(
        TContract record,
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken);

    /// <summary>Retires this implementation's row and its children, marking them deleted, inside the caller's unit of work.</summary>
    /// <param name="domain">The domain container the row hangs from.</param>
    /// <param name="domainKeyValues">The domain row's key values; the rows retired are those whose Foreign key to <paramref name="domain"/> holds them.</param>
    /// <param name="unit">The unit of work the domain provider opened.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that stopped it.</returns>
    Task<IGenericResult> Delete(
        IDataContainer domain,
        IReadOnlyDictionary<string, object?> domainKeyValues,
        ConfigurationWriteScope unit,
        CancellationToken cancellationToken);
}
