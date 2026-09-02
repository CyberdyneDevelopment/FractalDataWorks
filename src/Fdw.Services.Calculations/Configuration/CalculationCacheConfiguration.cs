using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// How calculation results are cached.
/// </summary>
/// <remarks>
/// Was the CalculationCache appsettings section. The per-type TTL overrides are a child container
/// rather than a column, the same way a connection's AdditionalProperties are: a list of values does
/// not belong in one field, and the schema has no array type.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Calculation", ServiceType = "CalculationCache")]
public sealed partial class CalculationCacheConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "Calculation";

    string IGenericConfiguration.ServiceType => "Calculation";

    string? IGenericConfiguration.ServiceOptionType => "CalculationCache";

    /// <summary>Gets or sets a value indicating whether calculation results are cached at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets how long a cached result stays valid, in minutes.</summary>
    public int DefaultTtlMinutes { get; set; }

    /// <summary>Gets or sets the longest TTL any calculation type may ask for, in minutes.</summary>
    public int MaxTtlMinutes { get; set; }

    /// <summary>Gets or sets a value indicating whether a data change evicts dependent results.</summary>
    public bool InvalidateOnDataChange { get; set; }

    /// <summary>Gets or sets a value indicating whether the cache is warmed at startup.</summary>
    public bool WarmupOnStartup { get; set; }

    /// <summary>Gets or sets the largest result that may be cached, in bytes.</summary>
    public int MaxCachedResultSizeBytes { get; set; }

    /// <summary>Gets or sets which cache implementation backs calculation results.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the prefix every calculation cache key carries.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the per-calculation-type TTL overrides, in minutes.</summary>
    /// <remarks>Name is the calculation type, Value its TTL. Values live in the child container,
    /// not in a column on this row.</remarks>
    [NotMapped]
    [ConfigurationChildTable("CalculationCacheTtl")]
    public IDictionary<string, string?> TtlByCalculationType { get; set; }
        = new Dictionary<string, string?>(StringComparer.Ordinal);
}
