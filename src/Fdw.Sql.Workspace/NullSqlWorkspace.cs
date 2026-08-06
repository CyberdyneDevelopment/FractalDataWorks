using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Workspace;

/// <summary>Null-object workspace used when no .sqlproj is loaded.</summary>
public sealed class NullSqlWorkspace : ISqlWorkspace
{
    public static readonly NullSqlWorkspace Instance = new();

    private NullSqlWorkspace() { }

    public TSqlModel Model => throw new InvalidOperationException("No SQL project loaded.");
    public string ProjectPath => string.Empty;
    public IReadOnlyList<string> ScriptPaths => Array.Empty<string>();
    public TSqlModel? BaselineModel => null;

    public string? GetScriptText(string path) => null;
    public void UpdateScript(string path, string newText) { }
    public string CreateSnapshot(string name, string description) => string.Empty;
    public IGenericResult<TSqlModel> RestoreSnapshot(string snapshotId) => GenericResult<TSqlModel>.Failure(SqlWorkspaceResultCodes.NoSqlProjectLoaded);
    public void SetBaseline() { }
    public int RevertToBaseline() => 0;
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<IReadOnlyList<string>>>(GenericResult<IReadOnlyList<string>>.Failure(SqlWorkspaceResultCodes.NoSqlProjectLoaded));
}
