using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The kinds of FROM source this endpoint reads in memory, one handler per parsed source type, found by the type's name.</summary>
/// <remarks>A source no handler reads is refused by <see cref="InMemorySelect"/> as a statement SQL Server does not support here.</remarks>
[TypeCollection(typeof(TableReferenceHandlerBase), typeof(ITableReferenceHandler), typeof(TableReferenceHandlers))]
[ExcludeFromCodeCoverage]
public abstract partial class TableReferenceHandlers : TypeCollectionBase<TableReferenceHandlerBase, ITableReferenceHandler>
{
}
