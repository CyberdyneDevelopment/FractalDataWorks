using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by type options search.
/// </summary>
public sealed class TypeOptionsData
{
    /// <summary>
    /// Gets or sets the total count of type options.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the collection filter applied.
    /// </summary>
    public required string CollectionFilter { get; init; }

    /// <summary>
    /// Gets or sets the list of type options.
    /// </summary>
    public required IReadOnlyList<TypeOptionInfo> TypeOptions { get; init; }
}