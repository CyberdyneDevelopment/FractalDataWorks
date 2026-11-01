using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Runs a query a person wrote and answers its rows (FDW-1435), each row its columns by name.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScriptedDataCommands), "Read", RestrictToCurrentCompilation = true)]
public sealed class ScriptReadCommand : ScriptedDataCommandBase<IReadOnlyList<IDictionary<string, object?>>>, IScriptReadCommand
{
    /// <summary>Initializes a new instance of the <see cref="ScriptReadCommand"/> class.</summary>
    /// <param name="text">The query.</param>
    /// <param name="parameters">The values of its named parameters.</param>
    /// <param name="take">The most rows to read; null states no limit.</param>
    public ScriptReadCommand(string text, IReadOnlyList<ScriptParameter> parameters, int? take)
        : base("Read", text, parameters)
    {
        Take = take;
    }

    /// <inheritdoc />
    public int? Take { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        Take < 1 ? $"the script may read at most {Take} rows, which is not at least one" : base.FirstProblem();
}
