using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>A floating point number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "Double")]
public sealed class ThresholdDoubleDataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdDoubleDataType"/> class.</summary>
    public ThresholdDoubleDataType()
        : base("Double")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;

    /// <inheritdoc />
    public override JsonValue? Scalar(string text) => Read(text) is double number ? JsonValue.Create(number) : JsonValue.Create(text);
}
