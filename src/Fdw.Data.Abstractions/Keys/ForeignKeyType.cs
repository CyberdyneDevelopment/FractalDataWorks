using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Foreign key — FK constraint referencing a parent container's field.
/// Generates FK constraint. Translator resolves parent RowId via subquery.
/// Lineage graph uses this for upstream/downstream edges.
/// </summary>
/// <remarks>
/// Retained for backward compatibility. Foreign-ness is now modelled as a non-null
/// <c>ReferencedKeyId</c> on a <see cref="LogicalKeyType"/> or <see cref="PhysicalKeyType"/> key.
/// Use Logical (with ReferencedKeyId set) for new logical-Id FK seed entries.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Foreign")]
public sealed class ForeignKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ForeignKeyType"/> class.</summary>
    public ForeignKeyType()
        : base(
            id: 3,
            name: "Foreign",
            isPrimaryKey: false,
            hasConstraint: true,
            isReference: true,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Foreign keys describe relationships to parent containers; they do not enforce
    /// uniqueness on the child side.
    /// </remarks>
    public override bool SupportsUniqueness => false;
}
