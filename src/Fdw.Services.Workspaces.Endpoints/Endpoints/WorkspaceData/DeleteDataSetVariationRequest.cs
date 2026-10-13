namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Names a variation a workspace owns, for deleting it: <c>DELETE /workspaces/{WorkspaceName}/data-sets/{Name}</c>.</summary>
public class DeleteDataSetVariationRequest
{
    /// <summary>Gets or sets the name of the workspace that owns the variation.</summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the variation's name, the one the workspace knows it by.</summary>
    public string Name { get; set; } = string.Empty;
}
