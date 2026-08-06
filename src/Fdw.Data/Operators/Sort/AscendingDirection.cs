using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Ascending sort direction (smallest to largest, A to Z).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SortDirections), "Ascending", RestrictToCurrentCompilation = true)]
public sealed class AscendingDirection : SortDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AscendingDirection"/> class.
    /// </summary>
    public AscendingDirection() : base(
        id: 1,
        name: "Ascending",
        description: "Sort from smallest to largest, A to Z",
        isAscending: true,
        sqlKeyword: "ASC")
    {
    }
}
