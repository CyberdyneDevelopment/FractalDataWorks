using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Singular composition — the dataset wraps a single source without structural transformation.
/// </summary>
/// <remarks>
/// Why: named <c>Singular</c> rather than <c>Single</c> to avoid CA1720 (identifier contains a type name).
/// </remarks>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetCompositionTypes), "Singular", RestrictToCurrentCompilation = true)]
public sealed class SingularCompositionType : DataSetCompositionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingularCompositionType"/> class.
    /// </summary>
    public SingularCompositionType()
        : base(
            id: 1,
            name: "Singular",
            description: "Wraps a single source without structural transformation",
            allowsMultipleSources: false,
            requiresJoins: false)
    {
    }
}
