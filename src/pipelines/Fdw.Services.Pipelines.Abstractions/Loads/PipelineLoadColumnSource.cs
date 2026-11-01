namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A destination column and where its value comes from (an entry of <c>valueSources</c>).</summary>
public sealed class PipelineLoadColumnSource
{
    /// <summary>Gets or sets the destination column.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets where the column's value comes from.</summary>
    public PipelineLoadValueSource? Source { get; set; }
}
