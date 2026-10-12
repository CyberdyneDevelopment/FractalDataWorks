namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The pipeline an edge passes through.</summary>
public sealed class LineageViaDto
{
    /// <summary>Gets or sets the pipeline's node id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the pipeline is enabled, or null when its configuration does not say.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the number of stages the pipeline has, or null when its configuration has no stage list.</summary>
    public int? StageCount { get; set; }

    /// <summary>Gets or sets a value indicating whether the caller may edit the pipeline (pipelines:write, and both its ends are visible).</summary>
    public bool CanEdit { get; set; }
}
