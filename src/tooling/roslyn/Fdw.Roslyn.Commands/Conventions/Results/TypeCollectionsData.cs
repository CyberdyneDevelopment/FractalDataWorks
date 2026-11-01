using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Data returned by type collections search.
/// </summary>
public sealed class TypeCollectionsData
{
    /// <summary>
    /// Gets or sets the total count of type collections.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets or sets the list of type collections.
    /// </summary>
    public required IReadOnlyList<TypeCollectionInfo> TypeCollections { get; init; }
}