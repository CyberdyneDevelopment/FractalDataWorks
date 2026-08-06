using System.Collections.Generic;

namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Response DTO containing the names of all configured secret managers.
/// </summary>
public sealed class ListSecretManagersResponse
{
    /// <summary>Gets or sets the names of the configured secret managers.</summary>
    public IReadOnlyList<string> Managers { get; set; } = [];
}
