namespace Fdw.Sql.Commands.Workspace.Translators;

/// <summary>Workspace summary returned from GetWorkspaceInfo.</summary>
public sealed class WorkspaceInfo
{
    /// <summary>Absolute path of the loaded .sqlproj.</summary>
    public string ProjectPath { get; set; } = string.Empty;
    /// <summary>Number of .sql scripts in the workspace.</summary>
    public int ScriptCount { get; set; }
    /// <summary>True if a baseline has been set.</summary>
    public bool HasBaseline { get; set; }
}
