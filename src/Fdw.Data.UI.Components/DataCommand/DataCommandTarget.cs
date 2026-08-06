using System;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Target container reference for write commands.</summary>
public sealed class DataCommandTarget
{
    /// <summary>Gets or sets the target container ID in ctrl metadata.</summary>
    public Guid ContainerId { get; set; }
}
