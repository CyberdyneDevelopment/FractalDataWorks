using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Request to set the paging a container of a data store states for itself, <c>PATCH /datastores/{Name}/containers/{ContainerId}/paging</c> (FDW-5562).</summary>
public class SetDataStoreContainerPagingRequest
{
    /// <summary>Gets or sets the data store's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container's identifier, from the route.</summary>
    public Guid ContainerId { get; set; }

    /// <summary>
    /// Gets or sets the paging style the container pages in: the name of an <c>HttpPagingStyles</c> option, which may be its connection's own. A container's
    /// settings are for the style it names, so the request names one.
    /// </summary>
    public string? PagingStyle { get; set; }

    /// <summary>
    /// Gets or sets the paging settings the container states, by name: <c>PagingPageSize</c>, <c>PagingMaxPages</c> and the keys its style reads. A setting left
    /// out, or sent blank, is taken from the connection when the container pages in its connection's style, and from the style's declared default otherwise.
    /// The request replaces the container's settings: what it does not state is no longer stated.
    /// </summary>
    public IDictionary<string, string?> PagingSettings { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
