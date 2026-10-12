using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Workspace;

/// <summary>
/// Singleton wrapper that the MCP host swaps via <see cref="SetActive"/>
/// after each successful load_sqlproject. All <see cref="ISqlWorkspace"/>
/// members delegate to the active instance, or to <see cref="NullSqlWorkspace.Instance"/>
/// when none is loaded.
/// </summary>
public sealed class ActiveSqlWorkspaceProxy : ISqlWorkspace
{
    private ISqlWorkspace _active = NullSqlWorkspace.Instance;

    /// <summary>Swap the active workspace. Returns the previous one for disposal.</summary>
    public ISqlWorkspace SetActive(ISqlWorkspace workspace)
    {
        var prev = _active;
        _active = workspace ?? throw new ArgumentNullException(nameof(workspace));
        return prev;
    }

    public TSqlModel Model => _active.Model;
    public string ProjectPath => _active.ProjectPath;
    public IReadOnlyList<string> ScriptPaths => _active.ScriptPaths;
    public TSqlModel? BaselineModel => _active.BaselineModel;
    public string? GetScriptText(string path) => _active.GetScriptText(path);
    public void UpdateScript(string path, string newText) => _active.UpdateScript(path, newText);
    public string CreateSnapshot(string name, string description) => _active.CreateSnapshot(name, description);
    public IGenericResult<TSqlModel> RestoreSnapshot(string snapshotId) => _active.RestoreSnapshot(snapshotId);
    public void SetBaseline() => _active.SetBaseline();
    public int RevertToBaseline() => _active.RevertToBaseline();
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default) => _active.ApplyChanges(cancellationToken);
}
