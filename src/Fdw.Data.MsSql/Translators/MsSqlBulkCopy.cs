using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql;

/// <summary>Runs a planned bulk copy into SQL Server (FDW-1152).</summary>
/// <remarks>
/// The one place a bulk copy is run, so the connection and its unit of work copy exactly alike: the same options and the same
/// column mapping, and the unit of work's transaction when there is one. A database error is thrown for the caller to turn into
/// the coded failure it gives any other command's database error.
/// </remarks>
public static class MsSqlBulkCopy
{
    private const int BatchSize = 1000;
    private const int TimeoutSeconds = 300;

    /// <summary>Copies the planned rows into the planned table.</summary>
    /// <param name="plan">What to copy and where.</param>
    /// <param name="connection">The open connection the copy runs on, with its session context already set.</param>
    /// <param name="transaction">The unit of work's transaction on that connection; null when the copy is not inside one.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    /// <returns>The number of rows copied.</returns>
    public static async Task<int> Write(
        MsSqlBulkCopyPlan plan,
        SqlConnection connection,
        SqlTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        // Safe defaults: constraints checked, triggers fired, identity values kept. No TableLock, for concurrency.
        const SqlBulkCopyOptions options = SqlBulkCopyOptions.CheckConstraints
            | SqlBulkCopyOptions.FireTriggers
            | SqlBulkCopyOptions.KeepIdentity;

        using var bulkCopy = new SqlBulkCopy(connection, options, transaction)
        {
            DestinationTableName = plan.Destination,
            BatchSize = BatchSize,
            BulkCopyTimeout = TimeoutSeconds
        };

        foreach (var column in plan.Columns)
        {
            bulkCopy.ColumnMappings.Add(column, column);
        }

        await bulkCopy.WriteToServerAsync(plan.Rows, cancellationToken).ConfigureAwait(false);
        return plan.Rows.Rows.Count;
    }
}
