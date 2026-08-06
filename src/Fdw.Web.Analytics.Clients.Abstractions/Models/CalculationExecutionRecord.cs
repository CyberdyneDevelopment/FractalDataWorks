using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Record of a calculation execution for analytics.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationExecutionRecord
{
    /// <summary>
    /// Gets or sets the calculation type.
    /// </summary>
    public required string CalculationType { get; init; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Gets or sets whether the calculation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets whether the result was from cache.
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Gets or sets the size of the input data.
    /// </summary>
    public int InputSize { get; init; }

    /// <summary>
    /// Gets or sets the user identifier (if available).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? Error { get; init; }
}
