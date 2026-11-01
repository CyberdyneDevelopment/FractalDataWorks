using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>One of a small set of values records are grouped by.</summary>
/// <remarks>Formerly PropertyRoles.Lookup. Grouped by and filtered by membership.</remarks>
[TypeOption(typeof(DatumTypes), "Categorical")]
[ExcludeFromCodeCoverage]
public sealed class CategoricalDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CategoricalDatumType"/> class.</summary>
    public CategoricalDatumType()
        : base(
            name: "Categorical",
            description: "One of a small set of values records are grouped by.",
            isKeyRole: false,
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
