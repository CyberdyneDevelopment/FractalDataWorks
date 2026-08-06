using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations.Abstractions.Caching;

/// <summary>
/// Options for a specific calculation cache entry.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationCacheEntryOptions
{
    /// <summary>
    /// Gets or sets the TTL in minutes for this entry.
    /// </summary>
    public int? TtlMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether this entry should use sliding expiration.
    /// </summary>
    public bool UseSlidingExpiration { get; set; }
}
