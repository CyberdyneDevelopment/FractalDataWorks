using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Logical key — key on a logical Id column (durable across versions).
/// </summary>
/// <remarks>
/// Why: Logical Id columns (e.g., <c>Id</c> on root tables, <c>{Parent}Id</c> on child tables)
/// are unique only among current versions. Lookups by logical Id must filter
/// <c>WHERE [col] = @value AND IsCurrent = 1</c> to resolve the current row.
/// <para>
/// Foreign-ness is expressed via <see cref="IContainerKey.ReferencedContainer"/> being non-null,
/// not by a separate TypeOption. A Logical key whose <c>ReferencedContainer</c> is set describes
/// a logical FK (i.e., a <c>{Parent}Id</c> FK to a parent's logical Id column).
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(KeyTypes), "Logical")]
public sealed class LogicalKeyType : KeyTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LogicalKeyType"/> class.</summary>
    public LogicalKeyType()
        : base(
            id: 7,
            name: "Logical",
            isPrimaryKey: true,
            hasConstraint: true,
            isReference: false,
            isSystemGenerated: false)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: Logical keys do not intrinsically enforce uniqueness on their own — a filtered
    /// unique index (WHERE IsCurrent = 1 AND IsDeleted = 0) enforces uniqueness per-current-version.
    /// The uniqueness is conditional, not absolute.
    /// </remarks>
    public override bool SupportsUniqueness => false;
}
