using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>The field has a question nobody has marked answered.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphFieldStates), "QuestionOpen")]
public sealed class QuestionOpenGraphFieldState : GraphFieldStateBase
{
    /// <summary>Initializes a new instance of the <see cref="QuestionOpenGraphFieldState"/> class.</summary>
    public QuestionOpenGraphFieldState() : base("QuestionOpen", precedence: 1)
    {
    }

    /// <inheritdoc />
    public override bool Applies(GraphDataSetFieldConfiguration field, bool dataSetHasIntendedSource) =>
        field.Notes.Any(note => !note.IsAnswered && GraphFieldNoteKinds.ByName(note.NoteKind).CanBeAnswered);
}
