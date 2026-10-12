using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>A descriptive value of a record: a name, a status, a colour.</summary>
/// <remarks>The detail a person reads. Matched as text as well as for equality.</remarks>
[TypeOption(typeof(DatumTypes), "Attribute")]
[ExcludeFromCodeCoverage]
public sealed class AttributeDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AttributeDatumType"/> class.</summary>
    public AttributeDatumType()
        : base(
            name: "Attribute",
            description: "Describes a record: a name, a status, a colour.",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: TextOperators)
    {
    }
}
