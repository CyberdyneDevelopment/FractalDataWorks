using System;
using Fdw.Data;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Generic configuration record with common fields for querying configuration tables.
/// </summary>
[GenerateMapper]
public sealed class ConfigurationRecord
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the service type.</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }
}
