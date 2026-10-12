using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Response body for the POST <c>datastores/-/discover</c> endpoint. Mirrors the server's
/// <c>DiscoveryResultPayload</c> summary (counts + messages); the endpoint returns discovery totals
/// for the named data store, not an enumerated container list.
/// </summary>
public sealed class DiscoveryResultPayload
{
    /// <summary>Gets or sets the data store that was discovered.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of paths discovered.</summary>
    public int PathCount { get; set; }

    /// <summary>Gets or sets the number of containers discovered.</summary>
    public int ContainersDiscovered { get; set; }

    /// <summary>Gets or sets the total number of containers discovered.</summary>
    public int ContainerCount { get; set; }

    /// <summary>Gets or sets the number of fields discovered.</summary>
    public int FieldsDiscovered { get; set; }

    /// <summary>Gets or sets the total number of fields discovered.</summary>
    public int FieldCount { get; set; }

    /// <summary>Gets or sets the timestamp of the discovery.</summary>
    public DateTime IntrospectedAt { get; set; }

    /// <summary>Gets or sets whether this was a refresh operation.</summary>
    public bool WasRefreshed { get; set; }

    /// <summary>Gets or sets any messages from the discovery process.</summary>
    public IList<string> Messages { get; set; } = [];
}
