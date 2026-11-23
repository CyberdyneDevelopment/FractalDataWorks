using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>True or false.</summary>
[TypeOption(typeof(RecordValueKinds), "Flag")]
[ExcludeFromCodeCoverage]
public sealed class FlagRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagRecordValueKind"/> class.</summary>
    public FlagRecordValueKind()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is bool;

    /// <inheritdoc />
    public override string Text(object value) => (bool)value ? "true" : "false";

    /// <inheritdoc />
    public override void WriteJson(Utf8JsonWriter writer, object value) => writer.WriteBooleanValue((bool)value);
}
