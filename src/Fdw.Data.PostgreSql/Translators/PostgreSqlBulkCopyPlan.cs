using System.Collections.Generic;

namespace Fdw.Data.PostgreSql;

/// <summary>What a COPY into PostgreSQL needs, planned from a bulk insert command (FDW-1152).</summary>
/// <param name="CopySql">The <c>COPY … FROM STDIN BINARY</c> statement.</param>
/// <param name="Columns">The names of the columns, in the order the statement lists them and each row is written.</param>
/// <param name="Rows">The rows to copy, each an entity or a name-to-value map.</param>
public sealed record PostgreSqlBulkCopyPlan(string CopySql, IReadOnlyList<string> Columns, IReadOnlyList<object> Rows);
