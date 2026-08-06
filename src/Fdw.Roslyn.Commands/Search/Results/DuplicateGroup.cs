using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Search.Results;

/// <summary>
/// Represents a group of duplicate code blocks.
/// </summary>
public sealed class DuplicateGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateGroup"/> class.
    /// </summary>
    public DuplicateGroup(string hash, IReadOnlyList<DuplicateCodeBlock> locations)
    {
        Hash = hash;
        Locations = locations;
    }

    /// <summary>
    /// Gets the hash of the duplicate code.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the locations of the duplicates.
    /// </summary>
    public IReadOnlyList<DuplicateCodeBlock> Locations { get; }

    /// <summary>
    /// Gets the count of duplicates.
    /// </summary>
    public int Count => Locations.Count;
}
