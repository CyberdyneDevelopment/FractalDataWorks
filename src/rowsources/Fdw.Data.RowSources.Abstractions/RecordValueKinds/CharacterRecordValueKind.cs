using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A single character, written as one-character text.</summary>
[TypeOption(typeof(RecordValueKinds), "Character")]
[ExcludeFromCodeCoverage]
public sealed class CharacterRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CharacterRecordValueKind"/> class.</summary>
    public CharacterRecordValueKind()
        : base("Character")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is char;

    /// <inheritdoc />
    public override string Text(object value) => ((char)value).ToString();
}
