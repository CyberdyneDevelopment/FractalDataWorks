using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>A moment with its offset.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdDataTypes), "DateTimeOffset")]
public sealed class ThresholdDateTimeOffsetDataType : ThresholdDataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdDateTimeOffsetDataType"/> class.</summary>
    public ThresholdDateTimeOffsetDataType()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text) => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment) ? moment : null;
}
