using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The JSON values a filter can carry as a scalar: a string, true, false or a number.</summary>
/// <remarks>A JSON value of any other kind answers the NotFound sentinel; the caller refuses it with a coded failure.</remarks>
[TypeCollection(typeof(JsonFilterScalarKindBase), typeof(IJsonFilterScalarKind), typeof(JsonFilterScalarKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class JsonFilterScalarKinds : TypeCollectionBase<JsonFilterScalarKindBase, IJsonFilterScalarKind>
{
}
