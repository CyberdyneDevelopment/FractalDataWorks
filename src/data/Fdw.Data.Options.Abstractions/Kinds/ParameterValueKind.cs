using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>The name of a parameter a run supplies. (FDW-6454).</summary>
[TypeOption(typeof(DataOptionValueKinds), "Parameter")]
public sealed class ParameterValueKind : DataOptionValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ParameterValueKind"/> class.</summary>
    public ParameterValueKind()
        : base("Parameter", "parameter")
    {
    }
}
