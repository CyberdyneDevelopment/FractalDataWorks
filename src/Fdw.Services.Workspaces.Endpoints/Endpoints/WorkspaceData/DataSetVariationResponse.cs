using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A variation as it was created.</summary>
public sealed class DataSetVariationResponse
{
    /// <summary>Gets or sets the variation's logical id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the variation's name: the value the data route takes after the workspace.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the variation's display name, when it has one.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the workspace that owns it.</summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the main data set it varies.</summary>
    public string BaseDataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the view this one is a view of, or null when its parent is the data set.</summary>
    public string? ParentName { get; set; }

    /// <summary>Gets or sets the view's stored filter tree, or null for none.</summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the variation's own data classification.</summary>
    public int ClassificationId { get; set; }
}
