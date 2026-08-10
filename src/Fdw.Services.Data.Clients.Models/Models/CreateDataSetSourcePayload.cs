using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create a DataSet source.
/// </summary>
public sealed class CreateDataSetSourcePayload
{
    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the DataStore name.</summary>
    public string DataStoreName { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection name.</summary>
    public string? ConnectionName { get; set; }
    /// <summary>Gets or sets the connection type.</summary>
    public string? ConnectionType { get; set; }
    /// <summary>Gets or sets the path within the DataStore.</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>Gets or sets the container name.</summary>
    public string ContainerName { get; set; } = string.Empty;
    /// <summary>Gets or sets whether pushdown is supported.</summary>
    public bool SupportsPredicatePushdown { get; set; }
    /// <summary>Gets or sets whether this is the primary source.</summary>
    public bool IsPrimary { get; set; }
    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; }
    /// <summary>Gets or sets the mapper type name.</summary>
    public string? MapperTypeName { get; set; }
    /// <summary>Gets or sets the HTTP endpoint.</summary>
    public string? HttpEndpoint { get; set; }
    /// <summary>Gets or sets the HTTP method.</summary>
    public string? HttpMethod { get; set; }
    /// <summary>Gets or sets the file path.</summary>
    public string? FilePath { get; set; }
    /// <summary>Gets or sets the file format.</summary>
    public string? FileFormat { get; set; }
    /// <summary>Gets or sets the container identifier resolved from the DataStore tree picker.</summary>
    public Guid? ContainerId { get; set; }
    /// <summary>Gets or sets the field mappings.</summary>
    public IList<DataSetFieldMappingPayload> FieldMappings { get; set; } = [];
}
