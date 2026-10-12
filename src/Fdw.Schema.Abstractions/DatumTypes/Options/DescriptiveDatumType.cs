using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Free text that explains a record: a description, a note, a comment.</summary>
/// <remarks>Read when someone asks for it, not on every read: a data command returns it only when it includes descriptions. Matched as text; never ordered or grouped by.</remarks>
[TypeOption(typeof(DatumTypes), "Descriptive")]
[ExcludeFromCodeCoverage]
public sealed class DescriptiveDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptiveDatumType"/> class.</summary>
    public DescriptiveDatumType()
        : base(
            name: "Descriptive",
            description: "Free text that explains a record: a description or a note.",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: false,
            isOrderable: false,
            isGroupable: false,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: TextOperators)
    {
    }

    /// <inheritdoc />
    /// <remarks>Only when the query includes descriptions.</remarks>
    public override bool IsReturned(bool includeMetadata, bool includeDescriptions, bool includeKeys) => includeDescriptions;
}
