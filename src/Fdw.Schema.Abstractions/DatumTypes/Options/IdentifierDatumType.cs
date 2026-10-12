using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A value that identifies a record: a natural key, a code, a business id.</summary>
/// <remarks>Formerly PropertyRoles.NaturalKey. Compared for equality and grouped by, never summed.</remarks>
[TypeOption(typeof(DatumTypes), "Identifier")]
[ExcludeFromCodeCoverage]
public sealed class IdentifierDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentifierDatumType"/> class.</summary>
    public IdentifierDatumType()
        : base(
            name: "Identifier",
            description: "Identifies a record: a natural key, a code or a business id.",
            isKeyRole: true,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: EqualityOperators)
    {
    }
}
