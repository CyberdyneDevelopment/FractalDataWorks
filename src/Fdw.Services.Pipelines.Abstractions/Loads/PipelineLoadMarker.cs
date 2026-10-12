namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A column and the value that marks a row, as the pipeline-loads contract states it (<c>marker</c>).</summary>
public sealed class PipelineLoadMarker
{
    /// <summary>Gets or sets the destination column that carries the marker.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the value that marks a row.</summary>
    public string Value { get; set; } = string.Empty;
}
