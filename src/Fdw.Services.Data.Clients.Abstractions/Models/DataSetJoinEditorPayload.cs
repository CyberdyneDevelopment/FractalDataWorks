using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Editor-state payload for a join between two DataSet sources during in-place workbench composition.
/// </summary>
public sealed class DataSetJoinEditorPayload
{
    /// <summary>Gets or sets the unique identifier for this join definition.</summary>
    public Guid JoinId { get; set; }

    /// <summary>Gets or sets the alias name of the left-hand source.</summary>
    public string LeftSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the alias name of the right-hand source.</summary>
    public string RightSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the join type (Inner, Left, Right, Full, Cross).</summary>
    public string JoinType { get; set; } = "Inner";

    /// <summary>Gets or sets the join key field name on the left source.</summary>
    public string LeftKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the join key field name on the right source.</summary>
    public string RightKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this join can be removed from the working set.</summary>
    public bool CanRemove { get; set; }
}
