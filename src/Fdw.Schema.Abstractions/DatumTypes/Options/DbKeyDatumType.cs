using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A physical key the database assigns: RowId and the *RowId columns that point at one.</summary>
/// <remarks>Formerly PropertyRoles.Surrogate. It means nothing outside the database, so it is never displayed, and a data command returns it only to server-side code that includes keys (a configuration read that joins or re-versions a row).</remarks>
[TypeOption(typeof(DatumTypes), "DbKey")]
[ExcludeFromCodeCoverage]
public sealed class DbKeyDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DbKeyDatumType"/> class.</summary>
    public DbKeyDatumType()
        : base(
            name: "DbKey",
            description: "A physical key the database assigns (RowId). Never displayed.",
            isKeyRole: true,
            isIndexable: true,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: false,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: false,
            allowedOperators: EqualityOperators)
    {
    }

    /// <inheritdoc />
    /// <remarks>Only when the query includes keys, which only server-side reads do; it is still never displayed.</remarks>
    public override bool IsReturned(bool includeMetadata, bool includeDescriptions, bool includeKeys) => includeKeys;
}
