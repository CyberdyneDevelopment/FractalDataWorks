using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql;

/// <summary>
/// A large set-based write planned for staging by bulk copy (FDW-4958): create the temporary table, copy the rows into it, then run the statement,
/// all on one connection and transaction.
/// </summary>
/// <param name="CreateSql">Creates the temporary table typed like the target's staged columns; it reads no rows (it starts with SET NOCOUNT ON).</param>
/// <param name="Copy">The rows to bulk copy into the temporary table, typed like the target.</param>
/// <param name="Statement">Indexes the temporary table on the keys, applies the set-based write and drops the table; its count is the rows written.</param>
public sealed record MsSqlStagedWrite(string CreateSql, MsSqlBulkCopyPlan Copy, SqlCommand Statement);
