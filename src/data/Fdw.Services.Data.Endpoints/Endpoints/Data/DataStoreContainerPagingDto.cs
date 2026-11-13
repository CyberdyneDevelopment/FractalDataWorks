using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>What a container of a data store states for its own paging, and the paging it reads with (FDW-5562).</summary>
public sealed class DataStoreContainerPagingDto
{
    /// <summary>Gets or sets the container's identifier.</summary>
    public Guid ContainerId { get; set; }

    /// <summary>Gets or sets the container's name.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the container states paging of its own: true when it names a paging style.</summary>
    public bool HasPagingOverride { get; set; }

    /// <summary>Gets or sets the paging style the container names, or null when it takes its connection's.</summary>
    public string? PagingStyle { get; set; }

    /// <summary>Gets or sets the paging settings the container states, by name. Empty when it states none.</summary>
    public IDictionary<string, string?> PagingSettings { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the paging the container reads with, each value with where it came from; null when the store does not page its containers or no style is stated at either level.</summary>
    public ContainerPagingDto? Paging { get; set; }
}
