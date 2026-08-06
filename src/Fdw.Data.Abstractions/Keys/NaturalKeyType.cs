using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Natural key — business identity (Name, Code, etc.).
/// Generates UNIQUE constraint. Used for lookups and deduplication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Natural")]
public sealed class NaturalKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NaturalKeyType"/> class.</summary>
    public NaturalKeyType()
        : base(
            id: 2,
            name: "Natural",
            isPrimaryKey: true,
            hasConstraint: true,
            isReference: false,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Natural keys do not intrinsically guarantee uniqueness across all entities
    /// (e.g., same Name in different scopes). Uniqueness is enforced at the container level
    /// via a UNIQUE constraint, not via the key type alone.
    /// </remarks>
    public override bool SupportsUniqueness => false;
}
