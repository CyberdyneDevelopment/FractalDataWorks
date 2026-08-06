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
/// <c>DataStoreConfiguration</c> (Paths → Containers → Fields → Keys) — the shape both
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
    /// The store configuration — a <c>DataStoreConfiguration</c>. The concrete builder downcasts to
    /// the configuration shape it understands and fails loud if the type is unexpected.
    /// </param>
    /// <returns>Success when the configuration was accepted; Failure (with MessageLogging) otherwise.</returns>
    IGenericResult Configure(IGenericConfiguration storeConfig);

    /// <summary>
    /// Builds the assembled <see cref="IDataStore"/> tree. May fetch field/key metadata from the
    /// backing store, so it is asynchronous.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token propagated to any backing-store fetch.</param>
    /// <returns>Success with the built store, or Failure (with MessageLogging) when assembly fails.</returns>
    Task<IGenericResult<IDataStore>> Build(CancellationToken cancellationToken = default);
}
