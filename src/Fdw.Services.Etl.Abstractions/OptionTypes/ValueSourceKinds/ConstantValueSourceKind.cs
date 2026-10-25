using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A fixed value of a stated type.</summary>
[TypeOption(typeof(ValueSourceKinds), "Constant")]
public sealed class ConstantValueSourceKind : ValueSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConstantValueSourceKind"/> class.</summary>
    public ConstantValueSourceKind() : base("Constant", mapsSourceColumn: false, requiresSourceField: false, requiresSequenceName: false, requiresSequenceOrder: false, requiresConstant: true)
    {
    }
}
