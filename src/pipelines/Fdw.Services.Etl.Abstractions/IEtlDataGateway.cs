using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The data gateway of the ETL server (FDW-1952): everything <see cref="IDataGateway"/> does, and the ability to read or write a declared
/// container at a different physical path or under a different name.
/// </summary>
/// <remarks>
/// Why: a file whose name is known only when the run starts (a drop, an event naming it) cannot be declared beforehand. The declared container
/// still supplies the shape (store, format, columns); the override replaces the address the connection uses. Only the ETL server has this
/// gateway, so a host whose gateway is not an <see cref="IEtlDataGateway"/> refuses a component that asks for an override and never ignores it.
/// </remarks>
public interface IEtlDataGateway : IDataGateway
{
    /// <summary>Executes a data command against a declared container, at a different physical path and/or name.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="target">The declared data store, path and container: the shape the connection parses by.</param>
    /// <param name="pathOverride">The physical path to use in place of the declared container's; none keeps the declared path.</param>
    /// <param name="containerNameOverride">The physical container name (the file) to use in place of the declared one; none keeps the declared name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command's result. A call with neither override is the same as <see cref="IDataGateway.Execute{T}(IDataCommand, DataStoreTarget, CancellationToken)"/>.</returns>
    Task<IGenericResult<T>> Execute<T>(
        IDataCommand command, DataStoreTarget target, string? pathOverride, string? containerNameOverride, CancellationToken cancellationToken = default);

    /// <summary>Opens a declared container's rows as a stream read in a stable order, at a different physical path and/or name (FDW-4911).</summary>
    /// <param name="command">The read: a query command.</param>
    /// <param name="target">The declared data store, path and container: the shape, keys and connection the read goes by.</param>
    /// <param name="pathOverride">The physical path to use in place of the declared container's; none keeps the declared path.</param>
    /// <param name="containerNameOverride">The physical container name (the file) to use in place of the declared one; none keeps the declared name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A disposable record source, read as <see cref="IDataGateway.OpenRecordSource(IDataCommand, DataStoreTarget, CancellationToken)"/> reads,
    /// at the address given. A call with neither override is the same as that member.
    /// </returns>
    Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, string? pathOverride, string? containerNameOverride, CancellationToken cancellationToken = default);
}
