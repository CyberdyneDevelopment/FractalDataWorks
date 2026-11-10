using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Scripts;

/// <summary>
/// What every SQL connection does the same way to run a script (FDW-1441): bind its named parameters to the driver's command,
/// and read the rows it selects as dictionaries keyed by column name.
/// </summary>
/// <remarks>
/// The driver-specific parts, opening the connection and putting the caller's context in front, stay with each connection; what
/// is shared lives here once, so a connection type that takes scripts adds the driver and nothing else.
/// </remarks>
public static class ScriptedRows
{
    /// <summary>Binds the script's values to the command as named parameters.</summary>
    /// <param name="command">The driver's command, its text already set.</param>
    /// <param name="parameters">The script's values; a null value is a database null.</param>
    /// <param name="prefix">What the driver puts before a parameter's name in the text (<c>@</c> for the SQL drivers).</param>
    public static void Bind(DbCommand command, IReadOnlyList<ScriptParameter> parameters, string prefix)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(prefix);
        foreach (var parameter in parameters)
        {
            var bound = command.CreateParameter();
            bound.ParameterName = prefix + parameter.Name;
            bound.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(bound);
        }
    }

    /// <summary>
    /// Reads the first result set: each row is its columns by name, a database null as null. A name twice in the result, or more
    /// rows than the script may read, fails the read rather than keeping part of it.
    /// </summary>
    /// <param name="reader">The open reader.</param>
    /// <param name="take">The most rows to read; null states no limit.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows, or the failure that says why the result cannot be kept whole.</returns>
    public static async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Read(
        DbDataReader reader, int? take, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(logger);
        if (RepeatedColumn(reader) is { } repeated)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(ScriptedRowsLog.ColumnRepeated(logger, repeated));
        }

        var names = new string[reader.FieldCount];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = reader.GetName(i);
        }

        var rows = new List<IDictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (take is { } most && rows.Count >= most)
            {
                return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(ScriptedRowsLog.TooManyRows(logger, most));
            }

            var row = new Dictionary<string, object?>(names.Length, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < names.Length; i++)
            {
                row[names[i]] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        ScriptedRowsLog.Read(logger, rows.Count);
        return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(rows);
    }

    /// <summary>
    /// Streams the first result set (FDW-4903): answers a record source that reads each row when it is asked for and owns the
    /// reader, its command and its connection until it ends or is disposed. A name twice in the result fails here, before any
    /// row is read, and what was handed over is released.
    /// </summary>
    /// <param name="reader">The open reader.</param>
    /// <param name="owned">What the source releases when it ends, in order: the reader, then its command, then its connection.</param>
    /// <param name="take">The most rows to read; the row after them ends the read as a failed record. Null states no limit.</param>
    /// <param name="failed">Says what a failure of the store while reading is, as the connection that ran the script says it.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The record source, or the failure that says why the result cannot be read.</returns>
    public static IGenericResult<IRecordSource<DataRecord>> Open(
        DbDataReader reader,
        IReadOnlyList<IDisposable> owned,
        int? take,
        Func<Exception, IEnumerable<IGenericMessage>> failed,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(owned);
        ArgumentNullException.ThrowIfNull(failed);
        ArgumentNullException.ThrowIfNull(logger);
        if (RepeatedColumn(reader) is { } repeated)
        {
            foreach (var release in owned)
            {
                release.Dispose();
            }

            return GenericResult<IRecordSource<DataRecord>>.Failure(ScriptedRowsLog.ColumnRepeated(logger, repeated));
        }

        return GenericResult<IRecordSource<DataRecord>>.Success(new ScriptedRecordSource(reader, owned, take, failed, logger));
    }

    // The first column whose name an earlier column already has, ignoring case; none when every name is its own.
    private static string? RepeatedColumn(DbDataReader reader)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!seen.Add(reader.GetName(i)))
            {
                return reader.GetName(i);
            }
        }

        return null;
    }
}
