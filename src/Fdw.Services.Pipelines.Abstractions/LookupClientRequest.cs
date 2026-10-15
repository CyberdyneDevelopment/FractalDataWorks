using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request for the Lookup-transform parameters on a create/update pipeline transform.
/// Field names mirror the server's <c>LookupRequest</c> exactly so the JSON round-trips.
/// </summary>
public class LookupClientRequest
{
    /// <summary>Gets or sets the lookup data set name.</summary>
    public string LookupDataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store the lookup reads, when it reads a container rather than a data set (FDW-1269).</summary>
    public string? LookupDataStoreName { get; set; }

    /// <summary>Gets or sets the path within that data store.</summary>
    public string? LookupDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path.</summary>
    public string? LookupContainerName { get; set; }

    /// <summary>Gets or sets the lookup key field (in the lookup source).</summary>
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the source key field to match against.</summary>
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional output field prefix.</summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>Gets or sets the lookup columns to bring across — one output field per column.</summary>
    public IReadOnlyList<string> LookupColumns { get; set; } = [];

    /// <summary>Gets or sets the join type name, resolved against <c>LookupJoinTypes</c>.</summary>
    public string JoinType { get; set; } = string.Empty;
}
