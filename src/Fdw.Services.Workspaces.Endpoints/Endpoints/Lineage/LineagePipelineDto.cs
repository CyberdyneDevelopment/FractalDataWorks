namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A pipeline that appears on an edge in a lineage answer.</summary>
public sealed class LineagePipelineDto
{
    /// <summary>Gets or sets the pipeline's node id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline's name.</summary>
    public string Name { get; set; } = string.Empty;
}
