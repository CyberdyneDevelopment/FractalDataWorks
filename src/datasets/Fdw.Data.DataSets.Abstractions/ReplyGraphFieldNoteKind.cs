using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A reply in the discussion on a field; it is never marked answered itself.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphFieldNoteKinds), "Reply")]
public sealed class ReplyGraphFieldNoteKind : GraphFieldNoteKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplyGraphFieldNoteKind"/> class.</summary>
    public ReplyGraphFieldNoteKind() : base("Reply", canBeAnswered: false)
    {
    }
}
