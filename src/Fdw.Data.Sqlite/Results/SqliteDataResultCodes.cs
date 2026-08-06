using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// TypeCollection for SQLite data command translator result codes.
/// </summary>
[TypeCollection(typeof(SqliteDataResultCodeBase), typeof(IResultCode), typeof(SqliteDataResultCodes))]
public abstract partial class SqliteDataResultCodes : TypeCollectionBase<SqliteDataResultCodeBase, IResultCode>
{
}
