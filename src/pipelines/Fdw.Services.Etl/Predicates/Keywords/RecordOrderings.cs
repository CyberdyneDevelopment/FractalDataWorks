using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// The comparison signs a record predicate states between a field and a value that decide by order: <c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c> and <c>&lt;</c>.
/// </summary>
/// <remarks>A predicate states a sign by its word, matched exactly through <c>ByWord</c>; a word that matches none gets <c>NotFound</c> and the predicate is refused. Equality is read by the text, not by order.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RecordOrderingBase), typeof(IRecordOrdering), typeof(RecordOrderings))]
public abstract partial class RecordOrderings : TypeCollectionBase<RecordOrderingBase, IRecordOrdering>
{
}
