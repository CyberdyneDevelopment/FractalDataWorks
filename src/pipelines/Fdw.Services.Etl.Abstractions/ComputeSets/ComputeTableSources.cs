using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The places a compute stage's table takes its rows from: the stage itself, a named data set or a named output.
/// </summary>
/// <remarks>A stage states a source by the option's name, matched exactly through <c>ByName</c>; a word that matches none is a problem of the stage, said by <see cref="ProblemOf"/>.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ComputeTableSourceBase), typeof(IComputeTableSource), typeof(ComputeTableSources))]
public abstract partial class ComputeTableSources : TypeCollectionBase<ComputeTableSourceBase, IComputeTableSource>
{
    /// <summary>Says what is wrong with a table that takes its rows from <paramref name="kind"/>, named or not.</summary>
    /// <param name="kind">The source word the stage states.</param>
    /// <param name="named">Whether the table states the name of its source.</param>
    /// <returns>The problem, or <c>null</c> when the table is right.</returns>
    public static string? ProblemOf(string? kind, bool named)
        => (kind is { } word ? ByName(word) : NotFound) is var source && !ReferenceEquals(source, NotFound)
            ? source.ProblemWith(named)
            : $"'{kind}' is not Stage, DataSet or Output";
}
