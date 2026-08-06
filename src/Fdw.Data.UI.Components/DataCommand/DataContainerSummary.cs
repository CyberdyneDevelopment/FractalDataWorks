using System;
using System.Collections.Generic;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>
/// Lightweight container descriptor used by <see cref="DataCommandContext"/> to populate
/// container-picker dropdowns without a full DataStore load.
/// </summary>
public sealed class DataContainerSummary
{
    /// <summary>Gets or sets the container's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical name (shown in the picker dropdown).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., "Table", "View").</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store name this container belongs to.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the path/schema segment this container lives under.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields declared in this container.</summary>
    public IReadOnlyList<DataFieldSummary> Fields { get; set; } = [];

    /// <summary>Gets the display label shown in dropdowns: <c>DataStore / Path / Name</c>.</summary>
    public string DisplayLabel =>
        string.IsNullOrEmpty(DataStoreName)
            ? Name
            : $"{DataStoreName} / {PathName} / {Name}";
}
