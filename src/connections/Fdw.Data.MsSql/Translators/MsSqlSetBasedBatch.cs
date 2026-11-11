using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// A SQL Server set-based write before it is staged (FDW-4958): the rows, the columns they carry, the statement that applies them from the
/// temporary table, and its parameters. It renders either inline (the rows as one JSON parameter, read by OPENJSON into the temporary table) or
/// staged (the rows bulk copied into the temporary table, which is indexed on the keys first).
/// </summary>
/// <param name="Command">The command, which already holds the statement's own parameters.</param>
/// <param name="Temp">The temporary table's name.</param>
/// <param name="Target">The quoted, qualified target table.</param>
/// <param name="Dialect">The dialect that quotes names.</param>
/// <param name="Staged">The columns each row carries, as the container declares them.</param>
/// <param name="Keys">The key columns, which the staged table is indexed on.</param>
/// <param name="Rows">The rows.</param>
/// <param name="Declarations">The DECLARE statements that precede the transaction (may be empty).</param>
/// <param name="Body">The statements that apply the rows from the temporary table.</param>
/// <param name="UseTransaction">Whether the body needs a transaction of its own when the caller has none.</param>
/// <param name="IdentityInsert">Whether a staged column is an identity column.</param>
internal sealed record MsSqlSetBasedBatch(
    SqlCommand Command,
    string Temp,
    string Target,
    ISqlDialect Dialect,
    IReadOnlyList<string> Staged,
    IReadOnlyList<string> Keys,
    IReadOnlyList<object> Rows,
    string Declarations,
    string Body,
    bool UseTransaction,
    bool IdentityInsert)
{
    /// <summary>Renders the inline form: the rows as one JSON parameter read by OPENJSON into the temporary table.</summary>
    /// <param name="problem">Why the rows cannot be staged, or <see langword="null"/>.</param>
    /// <returns>The command, or <see langword="null"/> when <paramref name="problem"/> is set.</returns>
    internal SqlCommand? Inline(out string? problem)
    {
        var json = MsSqlRowStaging.Serialize(Rows, Staged, out problem);
        if (json is null)
            return null;

        Command.CommandText = MsSqlLoadBatch.Wrap(
            Declarations, MsSqlRowStaging.Sql(Temp, Target, Staged, Dialect, IdentityInsert) + "SET NOCOUNT OFF; " + Body, Temp, UseTransaction);
        Command.Parameters.Add(new SqlParameter(MsSqlRowStaging.RowsParameter, SqlDbType.NVarChar, -1) { Value = json });
        return Command;
    }

    /// <summary>Renders the staged form: create the temporary table, bulk copy the rows into it, index it on the keys, apply the body.</summary>
    /// <param name="container">The target, whose field types the copied rows take.</param>
    /// <returns>The staged write, or the coded failure of a row a column cannot take.</returns>
    internal IGenericResult<MsSqlStagedWrite> AsStaged(IStorageContainer container)
    {
        var fields = Staged.Select(column => MsSqlLoadColumns.Find(container, column)
            ?? throw new InvalidOperationException($"Container {container.Name} does not declare column '{column}'")).ToList();
        var table = MsSqlInsertTranslator.ConvertToDataTable(Rows, fields, container.Name);
        if (table.IsFailure)
            return table.ToNewResult<MsSqlStagedWrite>();

        var columns = string.Join(", ", Staged.Select(Dialect.QuoteIdentifier));
        var index = $"CREATE CLUSTERED INDEX fdw_keys ON {Temp} ({string.Join(", ", Keys.Select(Dialect.QuoteIdentifier))}); ";
        Command.CommandText = MsSqlLoadBatch.Wrap(Declarations, index + "SET NOCOUNT OFF; " + Body, Temp, UseTransaction);
        return GenericResult<MsSqlStagedWrite>.Success(new MsSqlStagedWrite(
            $"SET NOCOUNT ON; SELECT TOP 0 {columns} INTO {Temp} FROM {Target};",
            new MsSqlBulkCopyPlan(table.Value!, Temp, Staged),
            Command));
    }
}
