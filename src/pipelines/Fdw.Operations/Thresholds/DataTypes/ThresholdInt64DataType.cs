using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>A whole number that fits a long.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "Int64")]
public sealed class ThresholdInt64DataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdInt64DataType"/> class.</summary>
    public ThresholdInt64DataType()
        : base("Int64")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) ? whole : null;

    /// <inheritdoc />
    public override JsonValue? Scalar(string text) => Read(text) is long whole ? JsonValue.Create(whole) : JsonValue.Create(text);
}
