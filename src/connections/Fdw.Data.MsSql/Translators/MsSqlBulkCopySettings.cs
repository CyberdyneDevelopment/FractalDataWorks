namespace Fdw.Data.MsSql;

/// <summary>How a SQL Server bulk copy runs (FDW-4959): the connection's own settings, or the defaults its connection type declares.</summary>
/// <param name="BatchSize">The rows sent to the server in each batch; at least one.</param>
/// <param name="TimeoutSeconds">The longest the copy may run, in whole seconds; at least one.</param>
/// <param name="TableLock">Whether the copy takes a table lock for its duration.</param>
public sealed record MsSqlBulkCopySettings(int BatchSize, int TimeoutSeconds, bool TableLock);
