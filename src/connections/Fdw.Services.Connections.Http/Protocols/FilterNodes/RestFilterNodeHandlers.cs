using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols.FilterNodes;

/// <summary>The REST filter node handlers, one for each filter node kind of <c>FilterNodeKinds</c> (Condition, Group), found by the kind's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RestFilterNodeHandlerBase), typeof(IRestFilterNodeHandler), typeof(RestFilterNodeHandlers))]
public abstract partial class RestFilterNodeHandlers : TypeCollectionBase<RestFilterNodeHandlerBase, IRestFilterNodeHandler>
{
}
