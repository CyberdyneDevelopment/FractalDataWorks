using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a source for a DataSet.
/// </summary>
public sealed class DataSetSourcePayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the DataStore name.</summary>
    public string DataStoreName { get; set; } = string.Empty;
    /// <summary>Gets or sets the connection name.</summary>
    public string? ConnectionName { get; set; }
    /// <summary>Gets or sets the connection type.</summary>
    public string? ConnectionType { get; set; }
    /// <summary>Gets or sets the path within the DataStore.</summary>
    /// <remarks>
    /// Named PathName and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this string and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathName { get; set; } = string.Empty;
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
    /// <summary>Gets or sets the field mappings.</summary>
    public IReadOnlyList<DataSetFieldMappingPayload> FieldMappings { get; set; } = Array.Empty<DataSetFieldMappingPayload>();

    /// <summary>Gets or sets the dataset this source feeds.</summary>
    /// <remarks>
    /// Present even when the source arrives nested under its dataset, where it is redundant.
    /// Identity is always knowable and a consumer that ignores it is unharmed; the alternative was
    /// a second type that carried it, which is how three copies of this shape came about.
    /// </remarks>
    public Guid DataSetId { get; set; }

    /// <summary>Gets or sets the container's identity, when one is resolved.</summary>
    public Guid? ContainerId { get; set; }

    /// <summary>Gets or sets the kind of source this is.</summary>
    public string SourceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the dataset this source draws from, when it is another dataset.</summary>
    public Guid? SourceDataSetId { get; set; }

    /// <summary>Gets or sets that dataset's name.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets a value indicating whether this source is active.</summary>
    public bool IsActive { get; set; }
}
