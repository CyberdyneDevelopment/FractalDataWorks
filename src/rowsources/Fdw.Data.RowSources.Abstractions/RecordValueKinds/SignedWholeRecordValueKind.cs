using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A whole number that fits a 64-bit signed integer.</summary>
[TypeOption(typeof(RecordValueKinds), "SignedWhole")]
[ExcludeFromCodeCoverage]
public sealed class SignedWholeRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SignedWholeRecordValueKind"/> class.</summary>
    public SignedWholeRecordValueKind()
        : base("SignedWhole")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is byte or sbyte or short or ushort or int or uint or long;

    /// <inheritdoc />
    public override string Text(object value) => Invariant(value);

    /// <inheritdoc />
    public override void WriteJson(Utf8JsonWriter writer, object value) => writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
