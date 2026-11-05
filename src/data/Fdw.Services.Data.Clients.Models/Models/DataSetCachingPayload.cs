namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents caching configuration for a DataSet.
/// </summary>
public sealed class DataSetCachingPayload
{
    /// <summary>Gets or sets whether caching is enabled. Left out, the data set kind's declared value applies (FDW-3956).</summary>
    public bool? Enabled { get; set; }
    /// <summary>Gets or sets the cache duration in minutes.</summary>
    public int? DurationMinutes { get; set; }
    /// <summary>Gets or sets the cache key pattern.</summary>
    public string? KeyPattern { get; set; }
}
