using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a path within a DataStore.
/// </summary>
public sealed class DataStorePathPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical path value.</summary>
    public string PhysicalPath { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the source-discovered description (e.g., from MS_Description).</summary>
    public string? SourceDescription { get; set; }
    /// <summary>Gets or sets the type of path.</summary>
    public string PathType { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of containers in this path.</summary>
    public IReadOnlyList<DataStoreContainerPayload> Containers { get; set; } = Array.Empty<DataStoreContainerPayload>();
}
