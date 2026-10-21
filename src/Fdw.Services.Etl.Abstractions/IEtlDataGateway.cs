using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
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
}
