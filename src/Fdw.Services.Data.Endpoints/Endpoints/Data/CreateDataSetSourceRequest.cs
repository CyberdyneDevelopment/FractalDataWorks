using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for a data source within a data set composition.
/// Mirrors the wizard's source payload shape.
/// </summary>
public class CreateDataSetSourceRequest
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
    /// <remarks>
    /// Named PathValue and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this value and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether predicate pushdown is supported.</summary>
    public bool SupportsPredicatePushdown { get; set; }

    /// <summary>Gets or sets whether this is the primary source.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Gets or sets the priority.</summary>
    public int Priority { get; set; }

    /// <summary>Gets or sets the mapper type name.</summary>
    public string? MapperTypeName { get; set; }

    /// <summary>Gets or sets the source kind discriminator ('DataStore', 'DataSet', 'Calculation').</summary>
    /// <remarks>Determines whether this source is a physical DataStore, another DataSet, or a derived calculation.</remarks>
    public string SourceKind { get; set; } = "DataStore";

    /// <summary>Gets or sets the source DataSet identifier when SourceKind='DataSet'.</summary>
    /// <remarks>Used for compound/federated DataSet joins; null for other source kinds.</remarks>
    public Guid? SourceDataSetId { get; set; }

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
}
