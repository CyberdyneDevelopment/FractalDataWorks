using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The kinds of single JSON value a graph setting's text can be read from: a string, a number, true and false.
/// </summary>
/// <remarks>A value is found by its JSON kind through <c>ByValueKind</c>; a kind that matches none (an object, an array, null) gets <c>NotFound</c>, which reads no text.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(JsonScalarTextBase), typeof(IJsonScalarText), typeof(JsonScalarTexts))]
public abstract partial class JsonScalarTexts : TypeCollectionBase<JsonScalarTextBase, IJsonScalarText>
{
}
