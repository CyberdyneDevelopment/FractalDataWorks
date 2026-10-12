using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using FluentValidation;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Caching configuration for dataset operations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CachingConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled.
    /// </summary>
    /// <value><c>true</c> if caching is enabled; otherwise, <c>false</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    /// <value>The number of minutes to cache data before expiration.</value>
    public int DurationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the cache key pattern.
    /// </summary>
    /// <value>Pattern for generating cache keys (may include placeholders).</value>
    public string KeyPattern { get; set; } = "dataset:{datasetName}:{queryHash}";
}