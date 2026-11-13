using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Base for the kinds of note in the discussion on a field out of a Graph data set.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name. Nothing persists that id;
/// <c>data.GraphDataSetFieldNote.NoteKind</c> holds the option's name.
/// </remarks>
public abstract class GraphFieldNoteKindBase : TypeOptionBase<GraphFieldNoteKindBase>, IGraphFieldNoteKind
{
    /// <summary>Initializes a new instance of the <see cref="GraphFieldNoteKindBase"/> class.</summary>
    /// <remarks>Used only by the source generator's Empty sentinel; a real option always supplies its parameters.</remarks>
    protected GraphFieldNoteKindBase()
        : this(string.Empty, false)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphFieldNoteKindBase"/> class.</summary>
    /// <param name="name">The kind name, stored in <c>data.GraphDataSetFieldNote.NoteKind</c>.</param>
    /// <param name="canBeAnswered">Whether a note of this kind is a question that can be marked answered.</param>
    protected GraphFieldNoteKindBase(string name, bool canBeAnswered)
        : base(name)
    {
        CanBeAnswered = canBeAnswered;
    }

    /// <inheritdoc />
    public bool CanBeAnswered { get; }
}
