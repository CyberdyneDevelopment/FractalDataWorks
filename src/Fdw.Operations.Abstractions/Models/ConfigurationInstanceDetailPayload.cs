using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Detailed configuration instance with property values.
/// </summary>
public sealed class ConfigurationInstanceDetailPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the instance name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the service type.</summary>
    public string ServiceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the configuration values.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }
}
