using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A number with a fraction: a float, a double or a decimal.</summary>
[TypeOption(typeof(RecordValueKinds), "Fractional")]
[ExcludeFromCodeCoverage]
public sealed class FractionalRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FractionalRecordValueKind"/> class.</summary>
    public FractionalRecordValueKind()
        : base("Fractional")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is float or double or decimal;

    /// <inheritdoc />
    public override string Text(object value) => Invariant(value);

    /// <inheritdoc />
    public override void WriteJson(Utf8JsonWriter writer, object value) => writer.WriteNumberValue(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
}
