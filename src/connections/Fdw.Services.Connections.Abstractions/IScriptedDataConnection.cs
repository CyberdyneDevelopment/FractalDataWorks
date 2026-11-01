using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// What takes a script, text a person wrote in the language of the store (SQL for a SQL database), with named parameters
/// (FDW-1435, FDW-1447).
/// </summary>
/// <remarks>
/// Every connection answers every data command; a scripted command is not one. The shared connections do not take scripts: a
/// script extension, one package for the ETL host alone, wraps a connection and implements this around it, and the
/// <see cref="ScriptedConnectionTypes"/> collection finds the extension for a store's connection. The DataGateway uses what
/// that finds, or refuses a store whose connection has no extension on this host. A script names no container: it runs against
/// the store the connection reaches, in the database the connection is configured with.
/// </remarks>
public interface IScriptedDataConnection
{
    /// <summary>
    /// Executes a scripted command.
    /// </summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The scripted command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The command's answer, or why it failed.</returns>
    Task<IGenericResult<T>> Execute<T>(IScriptedDataCommand<T> command, CancellationToken cancellationToken = default);
}
