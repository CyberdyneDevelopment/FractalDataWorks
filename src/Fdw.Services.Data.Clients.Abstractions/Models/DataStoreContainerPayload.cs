using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a container (table, file, etc.) within a path.
/// </summary>
public sealed class DataStoreContainerPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical name.</summary>
    public string PhysicalName { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of container.</summary>
    public string ContainerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the number of fields in this container.</summary>
    public int FieldCount { get; set; }
    /// <summary>Gets or sets the list of fields in this container.</summary>
    public IReadOnlyList<DataStoreFieldPayload> Fields { get; set; } = Array.Empty<DataStoreFieldPayload>();
    /// <summary>Gets or sets the list of operations supported by this container.</summary>
    public IReadOnlyList<string> SupportedOperations { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the source-discovered description.</summary>
    public string? SourceDescription { get; set; }
    /// <summary>Gets or sets the auto-generated key field names.</summary>
    public IReadOnlyList<string> SurrogateKeyFields { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the business key field names.</summary>
    public IReadOnlyList<string> NaturalKeyFields { get; set; } = Array.Empty<string>();
}
