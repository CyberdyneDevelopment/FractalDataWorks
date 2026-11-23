using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>Text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "String")]
public sealed class ThresholdStringDataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdStringDataType"/> class.</summary>
    public ThresholdStringDataType()
        : base("String")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => text;
}
