using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The one place a connection or a unit of work answers a write that reports its unmatched keys (FDW-4988): the command must opt in and the caller must ask
/// for a <see cref="BulkWriteAnswer"/>, else it is refused by name; the kind reads the batch's result sets apart and the answer is checked as it is read.
/// </summary>
public static class UnmatchedKeysAnswer
{
    /// <summary>Gets whether a command run for <typeparamref name="T"/> takes the answer path: it reports its unmatched keys, or a BulkWriteAnswer was asked for.</summary>
    /// <typeparam name="T">The result the caller asked for.</typeparam>
    /// <param name="command">The command.</param>
    /// <returns><see langword="true"/> when the answer path decides the result, refusals included.</returns>
    public static bool Applies<T>(IDataCommand command) =>
        command is IReportsUnmatchedKeys { UnmatchedKeySample: not null } || typeof(T) == typeof(BulkWriteAnswer);

    /// <summary>Answers a command that takes the answer path, reading the result sets with <paramref name="readSets"/>.</summary>
    /// <typeparam name="T">The result the caller asked for; only <see cref="BulkWriteAnswer"/> is answered.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="readSets">Runs the native command and reads its result sets apart, each row a dictionary of its columns.</param>
    /// <returns>The answer, or why it cannot be given.</returns>
    public static async Task<IGenericResult<T>> Execute<T>(
        IDataCommand command, ILogger logger, Func<Task<IGenericResult<IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>>>>> readSets)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));
        if (readSets is null)
            throw new ArgumentNullException(nameof(readSets));
        if (command is not IReportsUnmatchedKeys { UnmatchedKeySample: { } sample })
            return GenericResult<T>.Failure(ConnectionLogger.AnswerNotAskedFor(logger, command.CommandType));
        if (typeof(T) != typeof(BulkWriteAnswer))
            return GenericResult<T>.Failure(ConnectionLogger.UnmatchedKeysResultNotAnswer(logger, command.CommandType, typeof(T).Name));

        var sets = await readSets().ConfigureAwait(false);
        if (sets.IsFailure || sets.Value is null)
            return sets.ToNewResult<T>();

        var answer = BulkWriteAnswer.From(command.CommandType, sets.Value, sample);
        return answer.IsFailure ? answer.ToNewResult<T>() : GenericResult<T>.Success((T)(object)answer.Value!);
    }
}
