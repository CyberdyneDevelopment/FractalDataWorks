using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Descending sort direction (largest to smallest, Z to A).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SortDirections), "Descending", RestrictToCurrentCompilation = true)]
public sealed class DescendingDirection : SortDirectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DescendingDirection"/> class.
    /// </summary>
    public DescendingDirection() : base(
        id: 2,
        name: "Descending",
        description: "Sort from largest to smallest, Z to A",
        isAscending: false,
        sqlKeyword: "DESC")
    {
    }
}
