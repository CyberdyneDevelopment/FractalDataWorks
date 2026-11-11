using System.Collections.Generic;
using System.Data;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// The statements a SQL Server bulk update or delete adds when its command reports its unmatched keys (FDW-4988): before the write, count the staged keys
/// with no target row and keep the first of them; after it, select the summary set and the key set a <see cref="BulkWriteAnswer"/> is read from.
/// </summary>
/// <remarks>
/// The target is read under UPDLOCK, HOLDLOCK, so no row of a staged key appears or goes between the count and the write. A key whose row exists counts as
/// matched whatever its flag or marker, because the count asks only whether the key has a row. The staged keys are distinct: the rows were collapsed per key.
/// </remarks>
internal static class MsSqlUnmatchedKeys
{
    /// <summary>Wraps <paramref name="write"/> with the count, the sample and the answer's two result sets, or returns it as it is for a command that does not report.</summary>
    /// <param name="write">The write statement, ending with its semicolon.</param>
    /// <param name="sample">How many unmatched keys the answer carries, or <see langword="null"/> when the command does not report.</param>
    /// <param name="target">The quoted, qualified target table.</param>
    /// <param name="temp">The temporary table the keys are staged in.</param>
    /// <param name="keys">The key columns.</param>
    /// <param name="dialect">The dialect that quotes identifiers.</param>
    /// <param name="command">The command the sample size parameter is added to.</param>
    /// <returns>The statements to run.</returns>
    internal static string Around(string write, int? sample, string target, string temp, IReadOnlyList<string> keys, ISqlDialect dialect, SqlCommand command)
    {
        if (sample is not { } size)
            return write;

        command.Parameters.Add(new SqlParameter("@fdw_unmatched_sample", SqlDbType.Int) { Value = size });
        var columns = string.Join(", ", keys.Select(key => $"s.{dialect.QuoteIdentifier(key)}"));
        var on = string.Join(" AND ", keys.Select(key => $"t.{dialect.QuoteIdentifier(key)} = s.{dialect.QuoteIdentifier(key)}"));
        var absent = $"FROM {temp} AS s WHERE NOT EXISTS (SELECT 1 FROM {target} AS t WITH (UPDLOCK, HOLDLOCK) WHERE {on})";
        var kept = temp + "_um";
        return $"DECLARE @fdw_unmatched bigint = (SELECT COUNT_BIG(*) {absent}); "
               + $"SELECT TOP (@fdw_unmatched_sample) {columns} INTO {kept} {absent} ORDER BY {columns}; "
               + write
               + "DECLARE @fdw_affected bigint = @@ROWCOUNT; "
               + $"SELECT @fdw_affected AS {BulkWriteAnswer.AffectedColumn}, @fdw_unmatched AS {BulkWriteAnswer.UnmatchedColumn}; "
               + $"SELECT {string.Join(", ", keys.Select(dialect.QuoteIdentifier))} FROM {kept}; "
               + $"DROP TABLE {kept}; ";
    }
}
