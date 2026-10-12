using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Writes a result set: COLMETADATA with every column's settled type, then one ROW per row, sent as it fills
/// packets.
/// </summary>
/// <remarks>
/// A value that cannot be written as its column's type — a value longer than the length the column declares,
/// a number outside its range — is error 245 naming the column. The row it was in is taken back first, so the
/// client receives the rows before it, then the error.
/// </remarks>
public static class ResultSetWriter
{
    /// <summary>Writes a result set.</summary>
    /// <param name="context">The request.</param>
    /// <param name="output">The result.</param>
    /// <param name="line">The line of the statement that produced it.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>The rows written, or the refusal.</returns>
    public static async Task<IGenericResult<long>> Write(SqlExecutionContext context, SqlSelectOutput output, int line, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        var columns = Columns(context, output, line);
        if (columns.IsFailure)
            return columns.ToNewResult<long>();

        var payload = context.Response.Payload;
        TdsTokenWriter.ColumnMetadata(payload, columns.Value!, context.Server.Collation);

        long written = 0;
        foreach (var row in output.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mark = payload.Mark();
            if (!TdsTokenWriter.TryRow(payload, columns.Value!, row, out var failed))
            {
                payload.TruncateTo(mark);
                return SqlRefusal.Refuse<long>(new SqlServerMessage(245, 3, 16,
                    $"Conversion failed when writing a value of column '{columns.Value![failed].Name}' as {columns.Value[failed].Type}.", line), context.Logger);
            }

            written++;
            var flushed = await context.Response.Flush().ConfigureAwait(false);
            if (flushed.IsFailure)
                return flushed.ToNewResult<long>();
        }

        return GenericResult<long>.Success(written);
    }

    private static IGenericResult<IReadOnlyList<TdsColumn>> Columns(SqlExecutionContext context, SqlSelectOutput output, int line)
    {
        var columns = new List<TdsColumn>(output.Columns.Count);
        for (var i = 0; i < output.Columns.Count; i++)
        {
            var column = output.Columns[i];
            if (column.Name.Length > TdsColumn.MaxNameLength)
            {
                return SqlRefusal.Refuse<IReadOnlyList<TdsColumn>>(new SqlServerMessage(103, 1, 15,
                    $"The identifier that starts with '{column.Name[..TdsColumn.MaxNameLength]}' is too long. Maximum length is 128.", line), context.Logger);
            }

            var index = i;
            var type = SqlColumnTyping.Settle(column, output.Rows.Select(r => r[index]), context.Logger);
            if (type.IsFailure)
                return type.ToNewResult<IReadOnlyList<TdsColumn>>();

            columns.Add(new TdsColumn(column.Name, type.Value!, column.IsNullable));
        }

        return GenericResult<IReadOnlyList<TdsColumn>>.Success(columns);
    }
}
