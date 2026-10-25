using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Modifies a workspace.</summary>
/// <remarks>
/// Every field except the name is nullable, and null means "leave it alone". PATCH modifies what
/// it is given; a null Status must not be read as a request to blank the status.
/// </remarks>
public class UpdateWorkspaceRequest
{
    /// <summary>Gets or sets the name of the workspace to modify.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a new display name, or null to leave it.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets a new description, or null to leave it.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the workspace's data classification level (see
    /// <c>GET /data-classifications</c>). An id that is not a level is refused. Left out, the current classification stays.
    /// </summary>
    public int? ClassificationId { get; set; }

    /// <summary>Gets or sets a new purpose, or null to leave it.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets a new status, or null to leave it.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets a new visibility, or null to leave it.</summary>
    public string? Visibility { get; set; }

    /// <summary>Gets or sets a new join policy, or null to leave it.</summary>
    public string? JoinPolicy { get; set; }

    /// <summary>
    /// Gets or sets a new role for an AutoApprove workspace to grant, or null to leave it. Moving the join
    /// policy away from AutoApprove clears it; moving to AutoApprove requires one.
    /// </summary>
    public string? AutoApproveRole { get; set; }

    /// <summary>Gets or sets a new stand-in seed, or null to leave it.</summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the organization of the workspace's tenant to move it to, or null to leave it.</summary>
    public Guid? OrganizationId { get; set; }
}
