using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>The record query filter node handlers, one for each filter node kind of <c>FilterNodeKinds</c> (Condition, Group), found by the kind's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RowQueryNodeHandlerBase), typeof(IRowQueryNodeHandler), typeof(RowQueryNodeHandlers))]
public abstract partial class RowQueryNodeHandlers : TypeCollectionBase<RowQueryNodeHandlerBase, IRowQueryNodeHandler>
{
}
