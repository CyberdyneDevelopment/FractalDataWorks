using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Connections.PostgreSql.Results;

/// <summary>
/// TypeCollection for PostgreSQL connection result codes.
/// </summary>
[TypeCollection(typeof(PostgreSqlResultCodeBase), typeof(IResultCode), typeof(PostgreSqlResultCodes))]
public abstract partial class PostgreSqlResultCodes : TypeCollectionBase<PostgreSqlResultCodeBase, IResultCode>
{
}
