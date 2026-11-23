using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The kinds of WHERE condition that reach a data set's store, one handler per parsed condition type, found by the type's name.</summary>
/// <remarks>A condition no handler translates is refused by <see cref="PushdownFilter"/> with error 40517 naming it.</remarks>
[TypeCollection(typeof(PushdownConditionHandlerBase), typeof(IPushdownConditionHandler), typeof(PushdownConditionHandlers))]
[ExcludeFromCodeCoverage]
public abstract partial class PushdownConditionHandlers : TypeCollectionBase<PushdownConditionHandlerBase, IPushdownConditionHandler>
{
}
