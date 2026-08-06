using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Union composition — stacks rows from multiple sources using UNION semantics.
/// All sources must expose a compatible field shape.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetCompositionTypes), "Union", RestrictToCurrentCompilation = true)]
public sealed class UnionCompositionType : DataSetCompositionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionCompositionType"/> class.
    /// </summary>
    public UnionCompositionType()
        : base(
            id: 3,
            name: "Union",
            description: "Stacks rows from multiple sources using UNION semantics; sources must have compatible field shapes",
            allowsMultipleSources: true,
            requiresJoins: false)
    {
    }
}
