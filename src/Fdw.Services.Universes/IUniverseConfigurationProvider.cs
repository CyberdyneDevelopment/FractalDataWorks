using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Universes;

/// <summary>
/// Reads and writes universe configurations.
/// </summary>
/// <remarks>
/// Three Get overloads and nothing else: by name, by id, and all of them. A caller that wants
/// one universe's members reads the universe and walks it, rather than asking a provider for a
/// filtered slice — the aggregate the provider returns is already navigable.
/// </remarks>
public interface IUniverseConfigurationProvider
{
    /// <summary>Gets a universe by name, with its members, resources and relationships.</summary>
    /// <param name="name">The universe name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a universe by its logical identifier.</summary>
    /// <param name="id">The universe's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets every universe visible to the caller.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<UniverseConfiguration>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a universe and its children.</summary>
    /// <param name="record">The universe to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<UniverseConfiguration>> Save(UniverseConfiguration record, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a universe.</summary>
    /// <param name="id">The universe's logical identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default);
}
