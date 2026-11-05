using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A line between two groups: data moves from the first to the second through at least one pipeline.</summary>
public sealed class LineageLineDto
{
    /// <summary>Gets or sets the id of the group the pipelines read from.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the group the pipelines write to.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Gets or sets how many pipelines run along the line.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets the pipelines, by name, up to the <c>LineageLinePipelines</c> setting; <see cref="Count"/> says how many there are in all.</summary>
    public IReadOnlyList<LineagePipelineDto> Pipelines { get; set; } = [];
}
