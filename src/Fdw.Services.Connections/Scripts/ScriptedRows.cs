using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
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
        var names = new string[reader.FieldCount];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = reader.GetName(i);
            if (!seen.Add(names[i]))
            {
                return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(ScriptedRowsLog.ColumnRepeated(logger, names[i]));
            }
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
}
