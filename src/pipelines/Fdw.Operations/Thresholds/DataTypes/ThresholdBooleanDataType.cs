using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>A boolean.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "Boolean")]
public sealed class ThresholdBooleanDataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdBooleanDataType"/> class.</summary>
    public ThresholdBooleanDataType()
        : base("Boolean")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => bool.TryParse(text, out var flag) ? flag : null;

    /// <inheritdoc />
    public override JsonValue? Scalar(string text) => Read(text) is bool flag ? JsonValue.Create(flag) : JsonValue.Create(text);
}
