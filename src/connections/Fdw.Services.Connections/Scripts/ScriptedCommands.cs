using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Scripts;

/// <summary>
/// Sends a scripted command to the connection's reading of it (FDW-1441), the same way for every SQL connection: the command is
/// judged first, then a Read goes to the connection's read, a Run to its run and a Stream to its stream.
/// </summary>
public static class ScriptedCommands
{
    /// <summary>Runs a scripted command by the reading its kind has.</summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The scripted command.</param>
    /// <param name="read">Runs a Read and answers its rows.</param>
    /// <param name="run">Runs a Run and answers the rows it affected.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The command's answer, or why it was refused or failed.</returns>
    /// <remarks>A connection routed here does not stream: a Stream command is refused with <c>StreamNotRouted</c>, which says to route it here with the stream delegate.</remarks>
    public static Task<IGenericResult<T>> Route<T>(
        IScriptedDataCommand<T> command,
        Func<IScriptReadCommand, Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>> read,
        Func<IScriptRunCommand, Task<IGenericResult<int>>> run,
        ILogger logger)
    {
        return Dispatch(command, read, run, null, logger);
    }

    /// <summary>Runs a scripted command by the reading its kind has, for a connection that also streams a query's rows (FDW-4903).</summary>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The scripted command.</param>
    /// <param name="read">Runs a Read and answers its rows.</param>
    /// <param name="run">Runs a Run and answers the rows it affected.</param>
    /// <param name="stream">Runs a Stream and answers the record source its rows are read from; the caller disposes it.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The command's answer, or why it was refused or failed.</returns>
    public static Task<IGenericResult<T>> Route<T>(
        IScriptedDataCommand<T> command,
        Func<IScriptReadCommand, Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>> read,
        Func<IScriptRunCommand, Task<IGenericResult<int>>> run,
        Func<IScriptStreamCommand, Task<IGenericResult<IRecordSource<DataRecord>>>> stream,
        ILogger logger)
    {
        return Dispatch(command, read, run, stream, logger);
    }

    // Why a stream that may be none: the first Route is for a connection that does not stream, and a Stream sent to it is refused
    // below with its own message, which says what the connection must change.
    private static async Task<IGenericResult<T>> Dispatch<T>(
        IScriptedDataCommand<T> command,
        Func<IScriptReadCommand, Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>> read,
        Func<IScriptRunCommand, Task<IGenericResult<int>>> run,
        Func<IScriptStreamCommand, Task<IGenericResult<IRecordSource<DataRecord>>>>? stream,
        ILogger logger)
    {

        var problem = command.FirstProblem();
        if (problem is not null)
        {
            return GenericResult<T>.Failure(ScriptedCommandsLog.Refused(logger, command.CommandType, problem.Message));
        }

        // Why only Read, Run and Stream: they are the commands of the family, and each has an answer a connection reads. A command
        // added to the family is refused here, by name, until the connections read its answer.
        if (command is IScriptStreamCommand streaming)
        {
            if (stream is null)
            {
                return GenericResult<T>.Failure(ScriptedCommandsLog.StreamNotRouted(logger));
            }

            var source = await stream(streaming).ConfigureAwait(false);
            return source.IsSuccess && source.Value is { } opened ? GenericResult<T>.Success((T)(object)opened) : source.ToNewResult<T>();
        }

        if (command is IScriptReadCommand reading)
        {
            var rows = await read(reading).ConfigureAwait(false);
            return rows.IsSuccess && rows.Value is { } answer ? GenericResult<T>.Success((T)(object)answer) : rows.ToNewResult<T>();
        }

        if (command is IScriptRunCommand running)
        {
            var affected = await run(running).ConfigureAwait(false);
            return affected.IsSuccess ? GenericResult<T>.Success((T)(object)affected.Value) : affected.ToNewResult<T>();
        }

        return GenericResult<T>.Failure(ScriptedCommandsLog.NotRead(logger, command.CommandType));
    }
}
