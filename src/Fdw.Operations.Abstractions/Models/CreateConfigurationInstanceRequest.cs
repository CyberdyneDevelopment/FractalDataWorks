using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Request to create a configuration instance.
/// </summary>
public sealed class CreateConfigurationInstanceRequest
{
    /// <summary>Gets or sets the service type name.</summary>
    public string ServiceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the instance name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the configuration values.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
