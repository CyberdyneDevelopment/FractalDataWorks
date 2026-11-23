namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Makes a workspace the owner of a container: <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner</c> (FDW-6967).
/// </summary>
public class AssignDataStoreContainerOwnerRequest : AssignDataStoreItemOwnerRequest
{
    /// <summary>Gets or sets the container's name, from the route.</summary>
    public string ContainerName { get; set; } = string.Empty;
}
