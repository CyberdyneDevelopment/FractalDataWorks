using System.Collections.Generic;
using Fdw.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command that sends text a person wrote to a store that speaks it, with the values it needs as named parameters
/// (FDW-1435). It is not a <see cref="IDataCommand"/>: every connection answers a data command, and only a store that takes
/// a script, a SQL database, answers this one.
/// </summary>
public interface IScriptedDataCommand : IGenericCommand
{
    /// <summary>Gets the text to run, in the language of the store (SQL for a SQL database).</summary>
    string Text { get; }

    /// <summary>Gets the values of the script's named parameters. Values travel as parameters, never spliced into the text.</summary>
    IReadOnlyList<ScriptParameter> Parameters { get; }

    /// <summary>Lists what is wrong with the command itself, before any store is reached.</summary>
    /// <returns>The first problem in words, or null when the command is sound.</returns>
    string? FirstProblem();
}

/// <summary>A scripted command whose type parameter carries the result type, so a caller's Execute infers it.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public interface IScriptedDataCommand<TResult> : IScriptedDataCommand
{
    // Marker: the type parameter carries the result type, so a caller's Execute infers it.
}
