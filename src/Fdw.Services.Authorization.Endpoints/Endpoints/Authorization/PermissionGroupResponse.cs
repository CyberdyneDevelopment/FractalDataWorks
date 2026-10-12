using System;
using System.Collections.Generic;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>Response DTO for permissions grouped by domain.</summary>
public class PermissionGroupResponse
{
    /// <summary>Gets or sets the domain name (e.g., "connections", "datasets").</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the permissions for this domain.</summary>
    public IList<PermissionSummaryDto> Permissions { get; set; } = [];

    /// <summary>Stable id derived from the domain key; lets generic API consumers treat the group as a resource.</summary>
    public string Id => Domain;

    /// <summary>Alias for <see cref="Domain"/> for generic API consumers expecting a 'name' field.</summary>
    public string Name => Domain;
}
