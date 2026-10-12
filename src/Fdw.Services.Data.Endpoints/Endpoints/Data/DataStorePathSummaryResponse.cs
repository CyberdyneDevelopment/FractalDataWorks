using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a data path (schema) within a data store, without its containers (FDW-1194).
/// </summary>
/// <remarks>
/// Sent when the caller asks for a store's paths with <c>?containers=false</c>. It has no containers member at all, so
/// a path that has containers is never described as having none; the containers are read per path, paged.
/// </remarks>
public class DataStorePathSummaryResponse
{
    /// <summary>Gets or sets the path identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the path name (schema name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path type (e.g., Schema, Directory).</summary>
    public string PathType { get; set; } = string.Empty;

    /// <summary>Gets or sets the actual path value.</summary>
    public string PathValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the path description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the <c>ClassificationId</c> of the path's data classification level.</summary>
    public int ClassificationId { get; set; }
}
