using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>Data about the record rather than of it: who created or changed it and when, its tenant, its version flags.</summary>
/// <remarks>The audit block and system columns. Read when someone asks for it: a data command returns it only when it includes metadata. Filters on it still work, since a filter needs no selecting.</remarks>
[TypeOption(typeof(DatumTypes), "Metadata")]
[ExcludeFromCodeCoverage]
public sealed class MetadataDatumType : DatumTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MetadataDatumType"/> class.</summary>
    public MetadataDatumType()
        : base(
            name: "Metadata",
            description: "Data about the record: who created or changed it and when.",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: false,
            isOrderable: true,
            isGroupable: true,
            isPointInTime: false,
            isLowerDateBoundary: false,
            isUpperDateBoundary: false,
            isDisplayable: true,
            allowedOperators: RangeOperators)
    {
    }

    /// <inheritdoc />
    /// <remarks>Only when the query includes metadata.</remarks>
    public override bool IsReturned(bool includeMetadata, bool includeDescriptions, bool includeKeys) => includeMetadata;
}
