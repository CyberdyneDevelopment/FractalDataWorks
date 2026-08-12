using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a data path (schema) within a data store.
/// </summary>
public class DataStorePathResponse
{
    /// <summary>Gets or sets the path identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the path name (schema name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path type (e.g., Schema, Directory).</summary>
    public string PathType { get; set; } = string.Empty;

    /// <summary>Gets or sets the actual path value.</summary>
    /// <remarks>
    /// Named PathName and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this string and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the path description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the source-discovered description (e.g., from MS_Description).</summary>
    public string? SourceDescription { get; set; }

    /// <summary>Gets or sets the containers within this path.</summary>
    public IList<DataStoreContainerResponse> Containers { get; set; } = [];
}
