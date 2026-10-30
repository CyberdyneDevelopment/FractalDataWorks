using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Timeouts;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// Runs a command that declares an expansion as the simpler commands it expands into. A connection kind with no
/// translator for the command, and a unit of work on that kind, both use it, so the expansion is written once.
/// </summary>
public static class CommandExpansion
{
    /// <summary>
    /// Runs the commands <paramref name="command"/> expands into, in order, each through <paramref name="execute"/> (so a step may
    /// expand again), and answers together the rows the counted ones affected. The first failure ends it and is returned as it is.
    /// The commands are not one transaction unless <paramref name="execute"/> runs them in one.
    /// </summary>
    /// <typeparam name="T">The result type asked for: the row count.</typeparam>
    /// <param name="command">The command that declares the expansion.</param>
    /// <param name="container">The container the command targets.</param>
    /// <param name="execute">Runs one step of the expansion and answers the rows it affected.</param>
    /// <param name="logger">The logger for the connection or unit of work running the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rows the counted steps affected; or the failure that stopped the expansion.</returns>
    public static Task<IGenericResult<T>> Run<T>(
        IExpandableDataCommand command,
        IDataContainer container,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<int>>> execute,
        ILogger logger,
        CancellationToken cancellationToken = default)
        => Run<T>(command, container, execute, logger, null, cancellationToken);

    /// <summary>
    /// Runs the commands <paramref name="command"/> expands into as the other overload does, for a connection that has a
    /// <paramref name="maxSerialInsert"/>: a command that replaces rows in bulk above it expands that way.
    /// </summary>
    /// <typeparam name="T">The result type asked for: the row count.</typeparam>
    /// <param name="command">The command that declares the expansion.</param>
    /// <param name="container">The container the command targets.</param>
    /// <param name="execute">Runs one step of the expansion and answers the rows it affected.</param>
    /// <param name="logger">The logger for the connection or unit of work running the command.</param>
    /// <param name="maxSerialInsert">The most rows the connection runs serially before it takes its bulk route, or none when it has no such setting: a command that replaces rows in bulk above it then does it one row at a time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rows the counted steps affected; or the failure that stopped the expansion.</returns>
    public static async Task<IGenericResult<T>> Run<T>(
        IExpandableDataCommand command,
        IDataContainer container,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<int>>> execute,
        ILogger logger,
        int? maxSerialInsert,
        CancellationToken cancellationToken = default)
    {
        if (CommandTimeoutPolicy.TryGet(command, out _, out var timeoutOwner))
            return GenericResult<T>.Failure(ConnectionLogger.ExpansionCannotCarryCommandTimeout(logger, command.CommandType, timeoutOwner));

        if (typeof(T) != typeof(int))
            return GenericResult<T>.Failure(ConnectionLogger.ExpansionResultNotCount(logger, command.CommandType, typeof(T).Name));

        var expansion = maxSerialInsert is { } max && command is IBulkExpandableDataCommand bulk ? bulk.Expand(container, max) : command.Expand(container);
        if (expansion.IsFailure)
            return expansion.ToNewResult<T>();

        var steps = expansion.Value!;
        if (steps.Any(step => step.AnswersRows))
            return GenericResult<T>.Failure(ConnectionLogger.ExpansionNeedsAssembly(logger, command.CommandType));

        ConnectionLogger.TraceExpansionStarted(logger, command.CommandType, steps.Count);
        var affected = 0;
        foreach (var step in steps)
        {
            var done = await execute(step.Command, cancellationToken).ConfigureAwait(false);
            if (done.IsFailure)
                return done.ToNewResult<T>();

            affected += step.CountsAsAffected ? done.Value : 0;
        }

        return GenericResult<T>.Success((T)(object)affected);
    }

    /// <summary>
    /// Runs the commands <paramref name="command"/> expands into, in order, and answers the result the command puts
    /// together from what each step answered. A step marked as answering rows is run through <paramref name="rows"/>, any other
    /// through <paramref name="counted"/>. The first failure ends it and is returned as it is. The commands are not one
    /// transaction unless the two functions run them in one.
    /// </summary>
    /// <typeparam name="TResult">The result the command answers.</typeparam>
    /// <param name="command">The command that declares the expansion and assembles its result.</param>
    /// <param name="container">The container the command targets.</param>
    /// <param name="counted">Runs a step that answers a count.</param>
    /// <param name="rows">Runs a step that answers rows.</param>
    /// <param name="logger">The logger for the connection or unit of work running the command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command's assembled result; or the failure that stopped the expansion.</returns>
    public static async Task<IGenericResult<TResult>> RunAssembled<TResult>(
        IAssembledExpandableDataCommand<TResult> command,
        IDataContainer container,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<int>>> counted,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>> rows,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (CommandTimeoutPolicy.TryGet(command, out _, out var timeoutOwner))
            return GenericResult<TResult>.Failure(ConnectionLogger.ExpansionCannotCarryCommandTimeout(logger, command.CommandType, timeoutOwner));

        var expansion = command.Expand(container);
        if (expansion.IsFailure)
            return expansion.ToNewResult<TResult>();

        ConnectionLogger.TraceExpansionStarted(logger, command.CommandType, expansion.Value!.Count);
        var answers = new List<ExpansionAnswer>(expansion.Value.Count);
        foreach (var step in expansion.Value)
        {
            if (step.AnswersRows)
            {
                var read = await rows(step.Command, cancellationToken).ConfigureAwait(false);
                if (read.IsFailure)
                    return read.ToNewResult<TResult>();

                answers.Add(new ExpansionAnswer(0, read.Value));
                continue;
            }

            var done = await counted(step.Command, cancellationToken).ConfigureAwait(false);
            if (done.IsFailure)
                return done.ToNewResult<TResult>();

            answers.Add(new ExpansionAnswer(done.Value));
        }

        return command.Assemble(answers);
    }
}
