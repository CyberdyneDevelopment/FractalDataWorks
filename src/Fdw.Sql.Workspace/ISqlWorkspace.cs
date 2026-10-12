using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Workspace;

/// <summary>
/// SQL Server Data Tools (.sqlproj) workspace. Holds the parsed TSqlModel
/// plus the on-disk script files, with snapshot / baseline / apply-to-disk
/// semantics parallel to <c>Fdw.Workspace.Roslyn.IRoslynWorkspace</c>.
/// </summary>
public interface ISqlWorkspace
{
    /// <summary>Gets the current TSqlModel for the loaded .sqlproj.</summary>
    TSqlModel Model { get; }

    /// <summary>Gets the absolute path of the loaded .sqlproj.</summary>
    string ProjectPath { get; }

    /// <summary>Gets every .sql file path referenced by the project.</summary>
    IReadOnlyList<string> ScriptPaths { get; }

    /// <summary>Gets the current in-memory text for the given script path.</summary>
    string? GetScriptText(string path);

    /// <summary>Sets the in-memory text for the given script path.</summary>
    void UpdateScript(string path, string newText);

    /// <summary>Captures the current state as a named snapshot. Returns the snapshot ID.</summary>
    string CreateSnapshot(string name, string description);

    /// <summary>Restores the workspace to a previously captured snapshot.</summary>
    IGenericResult<TSqlModel> RestoreSnapshot(string snapshotId);

    /// <summary>Marks the current state as the comparison baseline.</summary>
    void SetBaseline();

    /// <summary>Gets the baseline TSqlModel, or null if no baseline has been set.</summary>
    TSqlModel? BaselineModel { get; }

    /// <summary>Writes every script whose current text differs from the last-applied snapshot to disk.</summary>
    Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default);

    /// <summary>Reverts every in-memory script to its baseline text.</summary>
    int RevertToBaseline();
}
