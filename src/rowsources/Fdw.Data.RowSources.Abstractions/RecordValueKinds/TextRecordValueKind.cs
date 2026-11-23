using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>Text, written as it is.</summary>
[TypeOption(typeof(RecordValueKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextRecordValueKind"/> class.</summary>
    public TextRecordValueKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string;

    /// <inheritdoc />
    public override string Text(object value) => (string)value;
}
