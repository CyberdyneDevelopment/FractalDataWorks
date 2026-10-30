using System.Collections.Generic;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// DTO for a registered secret manager type returned by ListSecretManagerTypesEndpointBase.
/// </summary>
public class SecretManagerTypeDto
{
    /// <summary>Gets or sets the type name key (e.g. "EnvironmentVariable", "MsSql").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name to show a person (e.g. "Environment Variable").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the type does, in the type's own words.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration container that holds this type's settings. Pass it as <c>type</c> to
    /// <c>GET /configuration/types/detail</c> to read each setting's label, whether it is required and
    /// whether it is a secret.
    /// </summary>
    public string SettingsContainer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the settings the kind takes, each with its label, whether it is required, whether it is a secret and its allowed values.
    /// The list is exactly the settings POST and PATCH /secret-managers accept.
    /// </summary>
    public IReadOnlyList<ConfigurationPropertyInfoDto> Settings { get; set; } = [];
}
