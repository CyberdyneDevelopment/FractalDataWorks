using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// The ways a calculation's input can be stated in JSON: a number, true, false, or a date written yyyy-MM-dd. A JSON value of any
/// other kind answers the NotFound sentinel, and the caller refuses it with a coded failure.
/// </summary>
[TypeCollection(typeof(JsonInputKindBase), typeof(IJsonInputKind), typeof(JsonInputKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class JsonInputKinds : TypeCollectionBase<JsonInputKindBase, IJsonInputKind>
{
}
