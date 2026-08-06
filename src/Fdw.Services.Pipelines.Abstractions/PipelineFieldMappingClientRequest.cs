namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request describing a single field mapping within a Map transform on a
/// create/update-pipeline request. Field names mirror the server's
/// <c>CreatePipelineFieldMappingRequest</c> exactly so the JSON round-trips.
/// </summary>
/// <remarks>
/// Why: plain data only — no <c>IFieldMapping</c> implementation here, to avoid this
/// client-abstractions package depending on <c>Fdw.Services.Etl.Abstractions</c>.
/// </remarks>
public class PipelineFieldMappingClientRequest
{
    /// <summary>
    /// Gets or sets the mapping name (display/identity within the transform).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source field name read from the input record.
    /// </summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination field name written to the output record.
    /// </summary>
    public string DestinationField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional target data type to coerce the value to (e.g. "long", "datetime").
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Gets or sets the optional named transform expression applied before TargetType coercion
    /// (e.g. "FromUnixMilliseconds").
    /// </summary>
    public string? TransformExpression { get; set; }

    /// <summary>
    /// Gets or sets whether the source field is required (a missing required field reports an error).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default value applied when the source field is null or missing.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this mapping is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
