using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A new UUIDv7 for each row written; the target column must hold a Guid.</summary>
[TypeOption(typeof(ValueSourceKinds), "UuidV7")]
public sealed class UuidV7ValueSourceKind : ValueSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UuidV7ValueSourceKind"/> class.</summary>
    public UuidV7ValueSourceKind() : base("UuidV7", mapsSourceColumn: false, requiresSourceField: false, requiresSequenceName: false, requiresSequenceOrder: false, requiresConstant: false, requiresGuidColumn: true)
    {
    }
}
