using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Translates a development command into an operation on a context (Solution, AST, etc.).
/// </summary>
public interface IDevelopmentCommandTranslator : ITypeOption<int, DevelopmentCommandTranslatorBase>
{
    /// <summary>
    /// Gets the type of command this translator handles.
    /// </summary>
    Type CommandType { get; }
}

/// <summary>
/// Strongly-typed translator for a specific command type and context.
/// </summary>
/// <typeparam name="TCommand">The type of command.</typeparam>
/// <typeparam name="TContext">The type of context (e.g., Solution for Roslyn).</typeparam>
/// <typeparam name="TResult">The type of result.</typeparam>
public interface IDevelopmentCommandTranslator<in TCommand, in TContext, TResult> : IDevelopmentCommandTranslator
    where TCommand : IDevelopmentCommand
    where TResult : IDevelopmentCommandResult
{
    /// <summary>
    /// Executes the command against the context.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="context">The context to operate on.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of command execution.</returns>
    Task<IGenericResult<TResult>> Execute(
        TCommand command,
        TContext context,
        CancellationToken cancellationToken = default);
}
