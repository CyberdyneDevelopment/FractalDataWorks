using System;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Summary information for a configuration instance.
/// </summary>
public sealed class ConfigurationInstanceSummaryResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the instance name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the service type.</summary>
    public required string ServiceType { get; set; }

    /// <summary>Gets or sets the category.</summary>
    public required string Category { get; set; }

    /// <summary>Gets or sets when the instance was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the instance was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}
