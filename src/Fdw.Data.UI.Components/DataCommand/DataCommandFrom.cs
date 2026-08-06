using System;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Primary FROM clause for a Query command.</summary>
public sealed class DataCommandFrom
{
    /// <summary>Gets or sets the container ID in ctrl metadata.</summary>
    public Guid ContainerId { get; set; }

    /// <summary>Gets or sets the alias used to qualify field references in this container.</summary>
    public string Alias { get; set; } = string.Empty;
}
