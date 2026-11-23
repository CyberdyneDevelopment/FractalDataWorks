using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.OData.FilterNodes;

/// <summary>The OData filter node handlers, one for each filter node kind of <c>FilterNodeKinds</c> (Condition, Group), found by the kind's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ODataFilterNodeHandlerBase), typeof(IODataFilterNodeHandler), typeof(ODataFilterNodeHandlers))]
public abstract partial class ODataFilterNodeHandlers : TypeCollectionBase<ODataFilterNodeHandlerBase, IODataFilterNodeHandler>
{
}
