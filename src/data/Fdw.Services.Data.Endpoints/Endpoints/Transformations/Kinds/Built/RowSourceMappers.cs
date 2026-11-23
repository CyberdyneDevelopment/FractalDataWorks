using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The kinds of rows source an output can state, each found by the name it is stored by.</summary>
/// <remarks>A name this collection does not hold answers the NotFound sentinel; the caller refuses with a coded failure.</remarks>
[TypeCollection(typeof(RowSourceMapperBase), typeof(IRowSourceMapper), typeof(RowSourceMappers))]
[ExcludeFromCodeCoverage]
public abstract partial class RowSourceMappers : TypeCollectionBase<RowSourceMapperBase, IRowSourceMapper>
{
}
