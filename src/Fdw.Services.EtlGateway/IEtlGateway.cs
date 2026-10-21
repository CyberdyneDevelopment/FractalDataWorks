using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.EtlGateway;

/// <summary>
/// The ETL host's own path to a data store's connection (FDW-1479): built for running pipelines, not for serving a console. It takes
/// the command and the store's name, resolves the connection and runs the command on it, with none of the data gateway's per-call
/// caching, limit counting or authorization of a person's request.
/// </summary>
/// <remarks>
/// Today it takes scripted commands only. Mike (2026-10-15): "the etl connections won't go through the data gateway", and an ETL
/// gateway "optimized for performance over convenience" may grow here; a pipeline's reads and loads still use the data gateway.
/// </remarks>
public interface IEtlGateway
{
    /// <summary>Runs a scripted command on a data store whose connection takes scripts.</summary>
    /// <remarks>
    /// The caller must hold the DataEngineer role. A store the host declares (configuration, identity, operations) is refused
    /// outright, since a script is arbitrary text. The store's connection is wrapped by the script extension this host references
    /// for that kind of connection (<c>ScriptedConnectionTypes</c>); a store whose connection has none here is refused. Connection
    /// limits do not apply: a query states its own most rows.
    /// </remarks>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The scripted command.</param>
    /// <param name="dataStore">The name of the data store to run it on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command's answer, or why it was refused or failed.</returns>
    Task<IGenericResult<T>> Execute<T>(IScriptedDataCommand<T> command, string dataStore, CancellationToken cancellationToken = default);
}
