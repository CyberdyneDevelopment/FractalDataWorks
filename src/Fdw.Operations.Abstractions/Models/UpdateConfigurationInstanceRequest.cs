using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Request to update a configuration instance.
/// </summary>
public sealed class UpdateConfigurationInstanceRequest
{
    /// <summary>Gets or sets the configuration values to update.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
}
