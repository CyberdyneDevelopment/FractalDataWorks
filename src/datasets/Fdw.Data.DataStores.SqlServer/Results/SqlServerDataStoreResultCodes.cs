using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// TypeCollection for SqlServer DataStore result codes.
/// Codes use the categorized-number catalog scheme (Id == EventId == number, Code == "SQLSERVER-{number}").
/// </summary>
[TypeCollection(typeof(SqlServerDataStoreResultCodeBase), typeof(IResultCode), typeof(SqlServerDataStoreResultCodes))]
public abstract partial class SqlServerDataStoreResultCodes : TypeCollectionBase<SqlServerDataStoreResultCodeBase, IResultCode>
{
}