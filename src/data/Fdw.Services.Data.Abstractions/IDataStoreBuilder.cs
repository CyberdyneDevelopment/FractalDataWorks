using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Per-transport builder that assembles one <see cref="IDataStore"/> tree (the uniform
/// <see cref="IDataNode"/> model: store → paths → containers → fields, with keys + FK-direct
/// key resolution) for a single transport.
/// </summary>
/// <remarks>
/// <para>
/// Each <c>DataStoreType</c> option supplies its own builder (MsSql builds
/// <c>MsSqlTableContainer</c>/<c>MsSqlViewContainer</c>; the generic builder serves Http/file),
/// replacing the three duplicate tree builders (<c>DataStoreTreeBuilder</c>,
/// <c>ConfigurationGateway.BuildFromSchema</c>, <c>DataStoreProvider.BuildCfgTierContainer</c>) and
/// the never-called <c>DataStoreTypeBase.Build</c>.
/// </para>
/// <para>
/// A true builder with ONE input source: <see cref="Configure"/> seeds it with a nested
/// <c>DataStoreImplementationConfiguration</c> (Paths → Containers → Fields → Keys) — the shape both
/// <c>ConfigurationSchema.DataStores</c> and the DB-loaded path already use — then <see cref="Build"/>
/// assembles the tree. There is no alternative node-by-node source.
/// </para>
/// <para>
/// Conventions: every fallible step returns <see cref="IGenericResult{T}"/>, never a
/// <c>Try*</c>/<c>bool</c>/nullable.
/// </para>
/// </remarks>
public interface IDataStoreBuilder
{
    /// <summary>
    /// Seeds the builder from a nested store configuration (its Paths/Containers/Fields/Keys).
    /// </summary>
    /// <param name="storeConfig">
    /// The store configuration — a <c>DataStoreImplementationConfiguration</c>. The concrete builder downcasts to
    /// the configuration shape it understands and fails loud if the type is unexpected.
    /// </param>
    /// <returns>Success when the configuration was accepted; Failure (with MessageLogging) otherwise.</returns>
    IGenericResult Configure(IGenericConfiguration storeConfig);

    /// <summary>
    /// Marks the store this builder builds as declared by the host in its <c>configurationSchema.json</c>
    /// (<see cref="IDataStore.DeclaredByHost"/>).
    /// </summary>
    /// <remarks>
    /// Called by the configuration gateway, which builds the host's declared stores, and by nothing else: whether a
    /// store is the host's is a fact about where it was loaded from, so the code that loads it says so.
    /// </remarks>
    void DeclareByHost();

    /// <summary>
    /// Declares that the configuration given to <see cref="Configure"/> is a slice of a store: only the containers named here are built whole, with their
    /// keys; any other container it holds is the bare node a key of a named one refers to (FDW-4328).
    /// </summary>
    /// <param name="containerNames">The containers the caller asked for.</param>
    /// <remarks>
    /// A slice's store lists only the named containers, so it is never handed out or kept as the store (<c>IDataStore.Paths</c> is the complete child
    /// set). The inbound keys of a named container are not resolved: its <c>ReferencingKeys</c> is a failure that says so.
    /// </remarks>
    void DeclareSlice(IReadOnlyCollection<string> containerNames);

    /// <summary>
    /// Builds the assembled <see cref="IDataStore"/> tree. May fetch field/key metadata from the
    /// backing store, so it is asynchronous.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated to any backing-store fetch.</param>
    /// <returns>Success with the built store, or Failure (with MessageLogging) when assembly fails.</returns>
    Task<IGenericResult<IDataStore>> Build(CancellationToken cancellationToken = default);
}
