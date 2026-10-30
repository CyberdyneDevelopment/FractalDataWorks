using System;
using System.Text.Json.Nodes;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Detail DTO for a single secret manager configuration.
/// </summary>
public sealed class SecretManagerDetailResponse
{
    /// <summary>Gets or sets the configuration ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the configuration name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret manager type.</summary>
    public string? SecretManagerType { get; set; }

    /// <summary>Gets or sets the service option type.</summary>
    public string? Implementation { get; set; }

    /// <summary>
    /// Gets or sets the settings the manager is set to now: the ones its kind takes, with each secret shown as the mask when it holds a value.
    /// Send the mask back in a PATCH to keep the stored secret.
    /// </summary>
    public JsonObject? Configuration { get; set; }
}
