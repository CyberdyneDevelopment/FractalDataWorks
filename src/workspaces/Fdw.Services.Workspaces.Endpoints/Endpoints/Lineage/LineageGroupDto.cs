namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A group in a lineage answer: a data-set category (or the data sets under none), or the containers of one data store path.</summary>
public sealed class LineageGroupDto
{
    /// <summary>Gets or sets the group's id: Category:&lt;path&gt;, StorePath:&lt;data store/path&gt;, or Uncategorised.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of group: Category, StorePath or Uncategorised.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the group's name: a category's own name, a store and path, or Uncategorised.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the group this one sits under; null at the top.</summary>
    public string? Parent { get; set; }

    /// <summary>Gets or sets how many data sets or containers sit directly in the group; a category that only holds categories is 0.</summary>
    public int Members { get; set; }
}
