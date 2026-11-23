using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Services.Data;

/// <summary>
/// Runs DDL on a connection by its name (FDW-3617): the connection's type says whether it can and does the running, as it does the
/// discovery for <see cref="ISchemaInformationService"/>. An endpoint calls this and holds no SQL, no engine name and no connection.
/// </summary>
public interface ISchemaDdlService
{
    /// <summary>
    /// Runs the commands on the named connection, in one unit of work.
    /// </summary>
    /// <param name="connectionName">The connection's name.</param>
    /// <param name="commands">The commands; the connection's type writes them in its own dialect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success once every command ran; otherwise the refusal: no such connection, a type that is not registered, a type that offers no
    /// DDL execution (<c>DdlExecutionNotOffered</c>), a connection that could not be opened, or the failure of the execution itself.
    /// </returns>
    Task<IGenericResult> Execute(
        string connectionName,
        IReadOnlyList<IDdlCommand> commands,
        CancellationToken cancellationToken = default);
}
