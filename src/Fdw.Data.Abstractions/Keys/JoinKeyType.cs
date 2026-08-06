using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Join key — logical relationship across containers or datasets with no FK constraint.
/// Used for cross-source DataSet joins, lineage edge discovery, and query planning.
/// No DB constraint generated — the relationship is metadata-only.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Join")]
public sealed class JoinKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="JoinKeyType"/> class.</summary>
    public JoinKeyType()
        : base(
            id: 4,
            name: "Join",
            isPrimaryKey: false,
            hasConstraint: false,
            isReference: true,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Join keys are logical relationships with no physical constraint; they impose
    /// no uniqueness guarantee.
    /// </remarks>
    public override bool SupportsUniqueness => false;
}
