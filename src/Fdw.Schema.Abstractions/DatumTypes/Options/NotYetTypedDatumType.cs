using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Nobody has said what this column's data is yet.</summary>
/// <remarks>The type every column carries until someone types it. It asserts nothing about the data, so it allows no filter operator until the column is typed, and is always returned: typing a column is what makes it filterable and changes how it is read.</remarks>
[TypeOption(typeof(DatumTypes), "NotYetTyped")]
[ExcludeFromCodeCoverage]
public sealed class NotYetTypedDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NotYetTypedDatumType"/> class.</summary>
    public NotYetTypedDatumType()
        : base(
            name: "NotYetTyped",
            description: "Not typed yet: the type every column carries until someone picks one.",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: NoOperators)
    {
    }
}
