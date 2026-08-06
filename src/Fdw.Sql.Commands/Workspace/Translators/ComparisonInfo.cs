using System;
using System.Collections.Generic;

namespace Fdw.Sql.Commands.Workspace.Translators;

/// <summary>Comparison report returned from CompareToBaseline.</summary>
public sealed class ComparisonInfo
{
    /// <summary>True if a baseline was set.</summary>
    public bool HasBaseline { get; set; }
    /// <summary>Number of script-level changes detected.</summary>
    public int ChangeCount { get; set; }
    /// <summary>Per-script change descriptions.</summary>
    public IReadOnlyList<string> Changes { get; set; } = Array.Empty<string>();
}
