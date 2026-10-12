namespace Fdw.Sql.Commands.Workspace.Translators;

/// <summary>Baseline status returned from GetBaseline.</summary>
public sealed class BaselineInfo
{
    /// <summary>True if a baseline has been set.</summary>
    public bool HasBaseline { get; set; }
    /// <summary>Number of scripts in the baseline (or current if none).</summary>
    public int ScriptCount { get; set; }
}
