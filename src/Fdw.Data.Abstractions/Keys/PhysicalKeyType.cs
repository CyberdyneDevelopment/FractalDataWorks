using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Physical key — key on a physical RowId column (DB-generated, globally unique forever).
/// </summary>
/// <remarks>
/// Why: RowId is a NEWSEQUENTIALID surrogate that is version-specific and globally unique —
/// no IsCurrent filter is needed or correct for RowId-based lookups. Use this type for keys
/// on RowId columns where point-lookup by physical identity is required.
/// Foreign-ness is expressed via <see cref="IContainerKey.ReferencedContainer"/> being non-null,
/// not by a separate TypeOption. A Physical key whose <c>ReferencedContainer</c> is set describes
/// a physical FK (i.e., a <c>RowId</c> FK to a parent's <c>RowId</c>).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Physical")]
public sealed class PhysicalKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PhysicalKeyType"/> class.</summary>
    public PhysicalKeyType()
        : base(
            id: 6,
            name: "Physical",
            isPrimaryKey: true,
            hasConstraint: true,
            isReference: false,
            isSystemGenerated: true)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Each physical key value is unique per row (DB-generated sequential/random GUID).
    /// </remarks>
    public override bool SupportsUniqueness => true;
}
