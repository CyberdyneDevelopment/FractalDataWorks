using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql.FilterNodes;

/// <summary>The SQL filter node handlers, one for each filter node kind of <c>FilterNodeKinds</c> (Condition, Group), found by the kind's name.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SqlFilterNodeHandlerBase), typeof(ISqlFilterNodeHandler), typeof(SqlFilterNodeHandlers))]
public abstract partial class SqlFilterNodeHandlers : TypeCollectionBase<SqlFilterNodeHandlerBase, ISqlFilterNodeHandler>
{
}
