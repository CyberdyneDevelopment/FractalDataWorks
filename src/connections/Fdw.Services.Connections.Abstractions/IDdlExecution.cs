using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// What a connection type offers when DDL can be run on a connection of its kind (FDW-3617): the commands of <c>Fdw.Schema.Ddl</c>, written
/// in the kind's own dialect (<see cref="IConnectionType.DdlGenerator"/>) and run on the live connection.
/// </summary>
/// <remarks>
/// <para>
/// Why a capability of the connection type and not a data command: DDL names a table that may not exist yet, so it has no container for the
/// DataGateway to resolve; it is an operation on the connection, like discovery (<c>ISchemaDiscovery</c>) and
/// <see cref="IDatabaseGenerationConnection"/>. The SQL runs in the connection implementation, where the engine's driver is.
/// </para>
/// <para>
/// A kind that does not implement this offers no DDL execution, and a request for it is refused with the coded <c>DdlExecutionNotOffered</c>:
/// no other kind's text is run in its place.
/// </para>
/// </remarks>
public interface IDdlExecution
{
    /// <summary>
    /// Runs the commands, in order and in one unit of work: either all of them are applied or none is.
    /// </summary>
    /// <param name="connection">The live connection to run them on; one that is not of this kind is refused.</param>
    /// <param name="commands">The commands, written by this kind's own generator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success once every command ran, or the failure of the first that did not.</returns>
    Task<IGenericResult> ExecuteDdl(
        IGenericConnection connection,
        IReadOnlyList<IDdlCommand> commands,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed variant of <see cref="IDdlExecution"/>, implemented by the connection type of one kind of connection.
/// </summary>
/// <typeparam name="TConnection">The kind's connection implementation.</typeparam>
public interface IDdlExecution<in TConnection> : IDdlExecution
    where TConnection : IGenericConnection
{
    /// <summary>
    /// Runs the commands on a typed connection; see <see cref="IDdlExecution.ExecuteDdl(IGenericConnection, IReadOnlyList{IDdlCommand}, CancellationToken)"/>.
    /// </summary>
    /// <param name="connection">The live, typed connection to run them on.</param>
    /// <param name="commands">The commands, written by this kind's own generator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success once every command ran, or the failure of the first that did not.</returns>
    Task<IGenericResult> ExecuteDdl(
        TConnection connection,
        IReadOnlyList<IDdlCommand> commands,
        CancellationToken cancellationToken = default);
}
