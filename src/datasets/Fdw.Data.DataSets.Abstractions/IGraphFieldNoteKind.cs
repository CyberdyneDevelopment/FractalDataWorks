using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A kind of note in the discussion on a field out of a Graph data set: what it is for.</summary>
public interface IGraphFieldNoteKind : ITypeOption<int, GraphFieldNoteKindBase>
{
    /// <summary>Gets a value indicating whether a note of this kind is a question that can be marked answered.</summary>
    bool CanBeAnswered { get; }
}
