namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Asks to join a workspace, for the caller.</summary>
/// <remarks>
/// There is no subject: a request is always for the person asking (FDW-1070). Bringing another user or
/// a whole role into a workspace is member management, which is decided by its owner or stewards.
/// </remarks>
public class RequestWorkspaceMembershipRequest
{
    /// <summary>Gets or sets the workspace name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role the caller asks for. A reviewer decides the role granted; an AutoApprove
    /// workspace grants the role it states, whatever was asked.
    /// </summary>
    public string RequestedRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the stated reason for the request.</summary>
    public string? Justification { get; set; }
}
