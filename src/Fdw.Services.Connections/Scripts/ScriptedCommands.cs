using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Scripts;

/// <summary>
/// Sends a scripted command to the connection's reading of it (FDW-1441), the same way for every SQL connection: the command is
/// judged first, then a Read goes to the connection's read and a Run to its run.
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
    public static async Task<IGenericResult<T>> Route<T>(
        IScriptedDataCommand<T> command,
        Func<IScriptReadCommand, Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>> read,
        Func<IScriptRunCommand, Task<IGenericResult<int>>> run,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(logger);

        var problem = command.FirstProblem();
        if (problem is not null)
        {
            return GenericResult<T>.Failure(ScriptedCommandsLog.Refused(logger, command.CommandType, problem));
        }

        // Why only Read and Run: they are the commands of the family, and each has an answer a connection reads. A command added
        // to the family is refused here, by name, until the connections read its answer.
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
