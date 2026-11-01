using System.Collections.Generic;
using System.Data;

namespace Fdw.Data.MsSql;

/// <summary>What a bulk copy into SQL Server needs, planned from a bulk insert command (FDW-1152).</summary>
/// <param name="Rows">The rows to copy, one DataTable column per mapped field.</param>
/// <param name="Destination">The schema-qualified table the rows are copied into.</param>
/// <param name="Columns">The names of the columns to map, the same on both sides.</param>
public sealed record MsSqlBulkCopyPlan(DataTable Rows, string Destination, IReadOnlyList<string> Columns);
