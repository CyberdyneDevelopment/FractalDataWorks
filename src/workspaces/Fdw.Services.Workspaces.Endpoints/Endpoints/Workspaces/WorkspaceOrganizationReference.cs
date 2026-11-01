using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>An organization, by id and name, as a workspace response names it.</summary>
public class WorkspaceOrganizationReference
{
    /// <summary>Gets or sets the organization's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the organization's name.</summary>
    public string Name { get; set; } = string.Empty;
}
