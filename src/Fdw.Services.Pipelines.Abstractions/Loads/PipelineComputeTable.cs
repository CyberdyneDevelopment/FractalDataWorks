namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>Where one table input of the Built transformation a Compute stage applies gets its rows.</summary>
public sealed class PipelineComputeTable
{
    /// <summary>Gets or sets the name of the table input the transformation declares.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Gets or sets where the rows come from: Stage (the rows arriving at the stage), DataSet or Output (a named output an earlier stage wrote).</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set or output name for DataSet or Output; null for Stage.</summary>
    public string? Name { get; set; }
}
