using System;
using Fdw.Data;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Database record representing a data source configured for a data set.
/// </summary>
[GenerateMapper]
public partial class DataSetSourceRecord
{
    /// <summary>Gets or sets the source record identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the parent data set identifier.</summary>
    public Guid DataSetId { get; set; }
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
    /// <summary>Gets or sets the path (schema/namespace) within the DataStore.</summary>
    public string? Path { get; set; }
    /// <summary>Gets or sets the container (table) name within the path.</summary>
    public string? ContainerName { get; set; }
    /// <summary>Gets or sets the HTTP endpoint URL.</summary>
    public string? HttpEndpoint { get; set; }
    /// <summary>Gets or sets the HTTP method.</summary>
    public string? HttpMethod { get; set; }
    /// <summary>Gets or sets the file path for file-based sources.</summary>
    public string? FilePath { get; set; }
    /// <summary>Gets or sets the file format.</summary>
    public string? FileFormat { get; set; }
}