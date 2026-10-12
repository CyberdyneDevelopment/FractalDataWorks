namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A field a Sort transform orders by (an entry of <c>sort</c>).</summary>
public sealed class PipelineSortField
{
    /// <summary>Gets or sets the field name.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the direction, Ascending or Descending.</summary>
    public string Direction { get; set; } = string.Empty;
}
