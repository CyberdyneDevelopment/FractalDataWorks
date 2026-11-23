namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Makes a workspace the owner of a path: <c>POST /datastores/{Name}/paths/{PathName}/owner</c> (FDW-6967). A container's route takes
/// <see cref="AssignDataStoreContainerOwnerRequest"/>.
/// </summary>
public class AssignDataStoreItemOwnerRequest
{
    /// <summary>Gets or sets the data store's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path's name, from the route.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the workspace to own it, from the body.</summary>
    public string Workspace { get; set; } = string.Empty;
}
