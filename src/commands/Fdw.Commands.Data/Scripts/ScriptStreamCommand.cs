using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;

namespace Fdw.Commands.Data;

/// <summary>
/// Runs a query a person wrote and answers a record source that streams its rows (FDW-4903): the rows are read as they are asked
/// for, so a large result is never held whole.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScriptedDataCommands), "Stream", RestrictToCurrentCompilation = true)]
public sealed class ScriptStreamCommand : ScriptedDataCommandBase<IRecordSource<DataRecord>>, IScriptStreamCommand
{
    /// <summary>Initializes a new instance of the <see cref="ScriptStreamCommand"/> class.</summary>
    /// <param name="text">The query.</param>
    /// <param name="parameters">The values of its named parameters.</param>
    /// <param name="take">The most rows to read; null states no limit.</param>
    public ScriptStreamCommand(string text, IReadOnlyList<ScriptParameter> parameters, int? take)
        : base("Stream", text, parameters)
    {
        Take = take;
    }

    /// <inheritdoc />
    public int? Take { get; }

    /// <inheritdoc />
    public override IGenericMessage? FirstProblem() =>
        Take < 1 ? Problem($"the script may read at most {Take} rows, which is not at least one") : base.FirstProblem();
}
