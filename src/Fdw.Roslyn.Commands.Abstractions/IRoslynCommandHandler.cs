using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Handles execution of Roslyn commands, orchestrating between workspace and translators.
/// </summary>
public interface IRoslynCommandHandler
{
    /// <summary>
    /// Executes a command and returns the result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <typeparam name="TResult">The type of result expected.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the command output or an error.</returns>
    Task<IGenericResult<TResult>> Execute<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRoslynCommand
        where TResult : IRoslynCommandResult;

    /// <summary>
    /// Executes a command with dynamic result type.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the command output or an error.</returns>
    Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        CancellationToken cancellationToken = default);
}
