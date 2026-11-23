using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A whole number that needs the 64-bit unsigned range.</summary>
[TypeOption(typeof(RecordValueKinds), "UnsignedWhole")]
[ExcludeFromCodeCoverage]
public sealed class UnsignedWholeRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UnsignedWholeRecordValueKind"/> class.</summary>
    public UnsignedWholeRecordValueKind()
        : base("UnsignedWhole")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is ulong;

    /// <inheritdoc />
    public override string Text(object value) => Invariant(value);

    /// <inheritdoc />
    public override void WriteJson(Utf8JsonWriter writer, object value) => writer.WriteNumberValue((ulong)value);
}
