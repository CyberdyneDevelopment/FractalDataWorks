using System;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Summary information for a configuration instance.
/// </summary>
public sealed class ConfigurationInstanceSummaryPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the instance name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the service type.</summary>
    public string ServiceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }
}
