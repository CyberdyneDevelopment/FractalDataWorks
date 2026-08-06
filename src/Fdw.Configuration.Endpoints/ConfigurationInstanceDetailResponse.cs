using System;
using System.Collections.Generic;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Detailed configuration instance with property values.
/// </summary>
public sealed class ConfigurationInstanceDetailResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the instance name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the service type.</summary>
    public required string ServiceType { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public required string Category { get; set; }

    /// <summary>Gets or sets the configuration values.</summary>
    public IDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Gets or sets when the instance was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the instance was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}
