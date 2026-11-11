using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql;

/// <summary>Runs a planned bulk copy into SQL Server (FDW-1152).</summary>
/// <remarks>
/// The one place a bulk copy is run, so the connection and its unit of work copy exactly alike: the same options and the same
/// column mapping, and the unit of work's transaction when there is one. A database error is thrown for the caller to turn into
/// the coded failure it gives any other command's database error. The batch size, timeout and table lock are the connection's
/// (<see cref="MsSqlBulkCopySettings"/>, FDW-4959); none is a constant here.
/// </remarks>
public static class MsSqlBulkCopy
{
    /// <summary>Copies the planned rows into the planned table.</summary>
    /// <param name="plan">What to copy and where.</param>
    /// <param name="settings">The connection's batch size, timeout and table lock.</param>
    /// <param name="connection">The open connection the copy runs on, with its session context already set.</param>
    /// <param name="transaction">The unit of work's transaction on that connection; null when the copy is not inside one.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    /// <returns>The number of rows copied.</returns>
    public static async Task<int> Write(
        MsSqlBulkCopyPlan plan,
        MsSqlBulkCopySettings settings,
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        // Constraints checked, triggers fired, identity values kept; a table lock only when the connection's settings ask for one.
        var options = SqlBulkCopyOptions.CheckConstraints
            | SqlBulkCopyOptions.FireTriggers
            | SqlBulkCopyOptions.KeepIdentity
            | (settings.TableLock ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default);

        using var bulkCopy = new SqlBulkCopy(connection, options, transaction)
        {
            DestinationTableName = plan.Destination,
            BatchSize = settings.BatchSize,
            BulkCopyTimeout = settings.TimeoutSeconds
        };

        foreach (var column in plan.Columns)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        await bulkCopy.WriteToServerAsync(plan.Rows, cancellationToken).ConfigureAwait(false);
        return plan.Rows.Rows.Count;
    }
}
