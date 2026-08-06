using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a data source configured for a data set.
/// </summary>
public class DataSetSourceResponse
{
    /// <summary>Gets or sets the source identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store name.</summary>
    public string? DataStoreName { get; set; }

    /// <summary>Gets or sets the connection name.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the connection type.</summary>
    public string? ConnectionType { get; set; }

    /// <summary>Gets or sets the source priority for ordering.</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets the HTTP endpoint URL.</summary>
    public string? HttpEndpoint { get; set; }

    /// <summary>Gets or sets the file path for file-based sources.</summary>
    public string? FilePath { get; set; }

    /// <summary>Gets or sets the path (schema) name within the DataStore.</summary>
    public string? PathName { get; set; }

    /// <summary>Gets or sets the container (table) name within the path.</summary>
    public string? ContainerName { get; set; }

    /// <summary>Gets or sets the container identifier resolved from the DataStore tree picker.</summary>
    public Guid? ContainerId { get; set; }

    /// <summary>Gets or sets the source kind discriminator ("DataStore" or "DataSet").</summary>
    public string SourceKind { get; set; } = "DataStore";

    /// <summary>Gets or sets the source DataSet identifier when <see cref="SourceKind"/> is "DataSet".</summary>
    public Guid? SourceDataSetId { get; set; }

    /// <summary>Gets or sets the source DataSet name, denormalized from <see cref="SourceDataSetId"/>.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets whether this is the primary source for a Compound dataset's pushed-down FROM clause.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Gets or sets whether this source is active.</summary>
    public bool IsActive { get; set; } = true;
}
