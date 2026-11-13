using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The kinds of note in the discussion on a field out of a Graph data set: a question and a reply (FDW-5294).</summary>
/// <remarks>
/// Extensible by design: <c>data.GraphDataSetFieldNote.NoteKind</c> carries no CHECK over the names, and its one CHECK binds only the kind it
/// names. No code switches on a kind's name; each states what it is through <see cref="IGraphFieldNoteKind"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphFieldNoteKindBase), typeof(IGraphFieldNoteKind), typeof(GraphFieldNoteKinds))]
public abstract partial class GraphFieldNoteKinds : TypeCollectionBase<GraphFieldNoteKindBase, IGraphFieldNoteKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
