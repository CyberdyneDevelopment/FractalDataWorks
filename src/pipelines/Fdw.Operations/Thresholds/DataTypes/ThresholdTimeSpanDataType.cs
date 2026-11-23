using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>A duration.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "TimeSpan")]
public sealed class ThresholdTimeSpanDataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdTimeSpanDataType"/> class.</summary>
    public ThresholdTimeSpanDataType()
        : base("TimeSpan")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var span) ? span : null;
}
