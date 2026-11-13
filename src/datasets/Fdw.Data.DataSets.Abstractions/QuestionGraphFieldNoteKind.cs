using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A question about a field; it stays open until it is marked answered.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphFieldNoteKinds), "Question")]
public sealed class QuestionGraphFieldNoteKind : GraphFieldNoteKindBase
{
    /// <summary>Initializes a new instance of the <see cref="QuestionGraphFieldNoteKind"/> class.</summary>
    public QuestionGraphFieldNoteKind() : base("Question", canBeAnswered: true)
    {
    }
}
