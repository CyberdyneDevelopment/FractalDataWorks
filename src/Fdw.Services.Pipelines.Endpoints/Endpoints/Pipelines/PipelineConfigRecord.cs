using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Internal record type for pipeline database table.
/// </summary>
public class PipelineConfigRecord
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline type.</summary>
    public string PipelineType { get; set; } = string.Empty;

    /// <summary>Gets or sets the source connection name.</summary>
    public string SourceConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination connection name.</summary>
    public string DestinationConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source DataSet name.</summary>
    public string? SourceDataSet { get; set; }

    /// <summary>Gets or sets the destination DataSet name.</summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets when the pipeline was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the pipeline was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
