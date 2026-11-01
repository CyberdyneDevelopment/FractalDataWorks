namespace Fdw.Sql.Commands.Workspace.Translators;

/// <summary>Snapshot data returned from CreateSnapshot.</summary>
public sealed class SnapshotInfo
{
    /// <summary>The persisted snapshot ID (patched by the handler).</summary>
    public string SnapshotId { get; set; } = string.Empty;
    /// <summary>Snapshot name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Snapshot description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Number of scripts captured.</summary>
    public int ScriptCount { get; set; }
}
