using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The next value of a named sequence, handed out in the order of the ordering columns.</summary>
[TypeOption(typeof(ValueSourceKinds), "NextSequenceValue")]
public sealed class NextSequenceValueValueSourceKind : ValueSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NextSequenceValueValueSourceKind"/> class.</summary>
    public NextSequenceValueValueSourceKind() : base("NextSequenceValue", mapsSourceColumn: false, requiresSourceField: false, requiresSequenceName: true, requiresSequenceOrder: true, requiresConstant: false)
    {
    }
}
