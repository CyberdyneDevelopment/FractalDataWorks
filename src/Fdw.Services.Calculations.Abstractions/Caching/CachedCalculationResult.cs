using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations.Abstractions.Caching;

/// <summary>
/// Represents a cached calculation result.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CachedCalculationResult
{
    /// <summary>
    /// Gets or sets the calculation type.
    /// </summary>
    public string CalculationType { get; set; } = "";

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public decimal Result { get; set; }

    /// <summary>
    /// Gets or sets the number of input values.
    /// </summary>
    public int InputCount { get; set; }

    /// <summary>
    /// Gets or sets when the result was cached.
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }

    /// <summary>
    /// Gets or sets when the result expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
