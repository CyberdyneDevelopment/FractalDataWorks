using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Development.Abstractions;
using Fdw.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Translates a Roslyn command into an operation on a Solution.
/// Extends <see cref="IDevelopmentCommandTranslator"/> for Roslyn-specific translation.
/// </summary>
public interface IRoslynCommandTranslator : IDevelopmentCommandTranslator
{
    /// <summary>
    /// Executes the command against the solution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="solution">The Roslyn solution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<IGenericResult<IRoslynCommandResult>> Execute(
        IRoslynCommand command,
        Solution solution,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed translator for a specific Roslyn command type.
/// </summary>
/// <typeparam name="TCommand">The type of command.</typeparam>
/// <typeparam name="TResult">The type of result.</typeparam>
public interface IRoslynCommandTranslator<in TCommand, TResult> : IRoslynCommandTranslator
    where TCommand : IRoslynCommand
    where TResult : IRoslynCommandResult
{
    /// <summary>
    /// Translates and executes the command against the solution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="solution">The Roslyn solution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The strongly-typed result.</returns>
    Task<IGenericResult<TResult>> Translate(
        TCommand command,
        Solution solution,
        CancellationToken cancellationToken = default);
}
