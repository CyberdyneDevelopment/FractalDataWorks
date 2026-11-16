using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.ColumnValues;

/// <summary>What <see cref="IContainerColumnValues.Search"/> is asked (FDW-5666).</summary>
public sealed class ColumnValuesQuery
{
    /// <summary>Gets the name of the data store that holds the container.</summary>
    public required string DataStore { get; init; }

    /// <summary>Gets the name of the path the container is under.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the name of the container.</summary>
    public required string Container { get; init; }

    /// <summary>Gets the name of the column whose values are searched; it must be one of the container's discovered fields.</summary>
    public required string Column { get; init; }

    /// <summary>
    /// Gets the text a value must contain, matched literally (a <c>%</c>, <c>_</c> or <c>[</c> in it is not a wildcard); null or
    /// empty matches every value.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>Gets how many matching values to pass over before the page starts.</summary>
    public int Skip { get; init; }

    /// <summary>
    /// Gets how many values the page holds; null takes the default the Main data gateway option declares. A take above that
    /// option's maximum is refused, never cut.
    /// </summary>
    public int? Take { get; init; }

    /// <summary>Gets the values already chosen; each answered value says whether it is one of them.</summary>
    public IReadOnlyList<string> Chosen { get; init; } = [];

    /// <summary>Gets whether the chosen values are left out of the page instead of being marked in it.</summary>
    public bool LeaveOutChosen { get; init; }
}
