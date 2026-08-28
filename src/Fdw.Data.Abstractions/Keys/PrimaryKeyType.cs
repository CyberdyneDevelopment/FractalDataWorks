using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Primary key — the logical durable identity column (Id).
/// Not DB-generated; set by the application. Child tables reference this column
/// via a Foreign key field whose ReferencedFieldRowId points here.
/// </summary>
/// <remarks>
/// Retained for backward compatibility with existing seed data. The key picker treats
/// PrimaryKey as equivalent to <see cref="LogicalKeyType"/>. Use Logical for new seed entries.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "PrimaryKey")]
public sealed class PrimaryKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="PrimaryKeyType"/> class.</summary>
    public PrimaryKeyType()
        : base(
            id: 5,
            name: "PrimaryKey",
            isPrimaryKey: true,
            hasConstraint: true,
            isReference: false,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    public override bool SupportsUniqueness => true;
}
