using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>A value stated in the rule. (FDW-6454).</summary>
[TypeOption(typeof(DataOptionValueKinds), "Literal")]
public sealed class LiteralValueKind : DataOptionValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="LiteralValueKind"/> class.</summary>
    public LiteralValueKind()
        : base("Literal", "value")
    {
    }
}
