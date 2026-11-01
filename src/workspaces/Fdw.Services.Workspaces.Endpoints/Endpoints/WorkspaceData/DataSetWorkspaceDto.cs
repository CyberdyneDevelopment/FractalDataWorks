namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One workspace a data set is attached to, as the caller may use it.</summary>
public sealed class DataSetWorkspaceDto
{
    /// <summary>Gets or sets the workspace's name — the value the data route takes.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the workspace's display name, when it has one.</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the name of the data set to ask this workspace's data route for: the data set asked about,
    /// or the variation of it the workspace owns.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether <see cref="DataSetName"/> is a variation the workspace owns, not the data set itself.</summary>
    public bool IsVariation { get; set; }

    /// <summary>Gets or sets whether <c>POST /{Name}/{DataSetName}</c> would serve the caller rows.</summary>
    public bool CanQuery { get; set; }
}
