using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// The ways a record predicate compares a field's text with a word: <c>contains</c>, <c>startswith</c> and <c>endswith</c>.
/// </summary>
/// <remarks>A predicate states a comparison by its word, matched exactly through <c>ByWord</c>; a word that matches none gets <c>NotFound</c> and the predicate is refused.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RecordTextMatchBase), typeof(IRecordTextMatch), typeof(RecordTextMatches))]
public abstract partial class RecordTextMatches : TypeCollectionBase<RecordTextMatchBase, IRecordTextMatch>
{
}
