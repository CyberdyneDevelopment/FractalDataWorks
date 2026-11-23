using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>
/// The command result values that carry a path (<c>DocumentInfo</c>, <c>DocumentPage</c>, <c>DocumentContent</c>), one handler for each,
/// found by the value's type name. A value with no handler carries no path and is returned as it is.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NarrowedResultHandlerBase), typeof(INarrowedResultHandler), typeof(NarrowedResultHandlers))]
public abstract partial class NarrowedResultHandlers : TypeCollectionBase<NarrowedResultHandlerBase, INarrowedResultHandler>
{
}
