using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Sql.Workspace;

/// <summary>Result codes emitted by <see cref="ISqlWorkspace"/>.</summary>
public static class SqlWorkspaceResultCodes
{
    public static readonly IResultCode ProjectNotFound    = new SqlWorkspaceResultCode(30000, nameof(ProjectNotFound),    ResultSeverities.ByName("Error"), "SQL project file was not found.");
    public static readonly IResultCode SnapshotIdRequired = new SqlWorkspaceResultCode(20000, nameof(SnapshotIdRequired), ResultSeverities.ByName("Error"), "Snapshot ID is required.");
    public static readonly IResultCode SnapshotNotFound   = new SqlWorkspaceResultCode(31000, nameof(SnapshotNotFound),   ResultSeverities.ByName("Error"), "Snapshot not found.");
    public static readonly IResultCode ApplyChangesFailed = new SqlWorkspaceResultCode(70002, nameof(ApplyChangesFailed), ResultSeverities.ByName("Error"), "One or more scripts could not be written to disk.");
    public static readonly IResultCode NoSqlProjectLoaded = new SqlWorkspaceResultCode(40000, nameof(NoSqlProjectLoaded), ResultSeverities.ByName("Error"), "No SQL project is loaded. Call load_sqlproject first.");
}
