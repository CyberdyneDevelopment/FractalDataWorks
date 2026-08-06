using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request for updating an existing pipeline.
/// </summary>
public class UpdatePipelineRequest
{
    /// <summary>
    /// Gets or sets the pipeline name (from route).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source connection name.
    /// </summary>
    public string? SourceConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the destination connection name.
    /// </summary>
    public string? DestinationConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the source DataSet name.
    /// </summary>
    public string? SourceDataSet { get; set; }

    /// <summary>
    /// Gets or sets the destination DataSet name.
    /// </summary>
    public string? DestinationDataSet { get; set; }

    /// <summary>
    /// Gets or sets optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the pipeline is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the replacement transform (operation) steps for this pipeline. Null means "leave the
    /// existing transforms unchanged"; a non-null (including empty) list replaces the whole set. Each
    /// entry maps onto a typed <c>PipelineTransformConfiguration</c> via
    /// <see cref="Fdw.Services.Pipelines.Endpoints.PipelineTransformConfigurationMapper"/>.
    /// </summary>
    public IList<CreatePipelineTransformRequest>? Transforms { get; set; }
}
