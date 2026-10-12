using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The time the load runs.</summary>
[TypeOption(typeof(ValueSourceKinds), "CurrentTimestamp")]
public sealed class CurrentTimestampValueSourceKind : ValueSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CurrentTimestampValueSourceKind"/> class.</summary>
    public CurrentTimestampValueSourceKind() : base("CurrentTimestamp", mapsSourceColumn: false, allowedAsContainerParameter: true, requiresSourceField: false, requiresSequenceName: false, requiresSequenceOrder: false, requiresConstant: false)
    {
    }
}
