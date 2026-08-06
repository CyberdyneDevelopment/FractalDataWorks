using System;

namespace Fdw.Services.SecretManagers.Clients.Models;

/// <summary>
/// Detail payload for a single secret manager configuration.
/// </summary>
public sealed class SecretManagerDetailPayload
{
    /// <summary>Gets or sets the configuration ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager type.</summary>
    public string? SecretManagerType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the service option type.</summary>
    public string? ServiceOptionType { get; set; }
}
