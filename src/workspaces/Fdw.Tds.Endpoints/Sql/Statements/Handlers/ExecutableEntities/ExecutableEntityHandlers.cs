using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>The things an EXEC can name, one handler per parsed entity type, found by the type's name.</summary>
/// <remarks>An entity no handler runs is refused by <see cref="ExecuteHandler"/> as a statement SQL Server does not support here.</remarks>
[TypeCollection(typeof(ExecutableEntityHandlerBase), typeof(IExecutableEntityHandler), typeof(ExecutableEntityHandlers))]
[ExcludeFromCodeCoverage]
public abstract partial class ExecutableEntityHandlers : TypeCollectionBase<ExecutableEntityHandlerBase, IExecutableEntityHandler>
{
}
