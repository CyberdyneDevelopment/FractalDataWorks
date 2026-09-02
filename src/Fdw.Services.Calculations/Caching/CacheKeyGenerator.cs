using System;
using Fdw.Services.Calculations.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fdw.Services.Calculations.Abstractions.Caching;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Calculations.Caching;

/// <summary>
/// Generates deterministic cache keys for calculation requests.
/// </summary>
public sealed class CacheKeyGenerator
{
    private readonly CalculationCacheConfiguration _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheKeyGenerator"/> class.
    /// </summary>
    /// <param name="options">The calculation cache options.</param>
    public CacheKeyGenerator(CalculationCacheConfiguration options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Generates a cache key for a calculation request.
    /// Pattern: {prefix}{definitionHash}:{paramHash}:{dataVersionHash}
    /// </summary>
    /// <param name="calculationType">The type of calculation (e.g., Sum, Average).</param>
    /// <param name="values">The input values for the calculation.</param>
    /// <param name="dataSourceVersion">Optional version identifier for the data source.</param>
    /// <returns>A deterministic cache key.</returns>
    public string Generate(string calculationType, decimal[] values, string? dataSourceVersion = null)
    {
        var definitionHash = ComputeHash(calculationType);
        var paramHash = ComputeValuesHash(values);
        return dataSourceVersion is null
            ? $"{_options.KeyPrefix}{definitionHash}:{paramHash}"
            : $"{_options.KeyPrefix}{definitionHash}:{paramHash}:{dataSourceVersion}";
    }

    /// <summary>
    /// Generates a hash for the calculation definition (type only for now).
    /// </summary>
    /// <param name="calculationType">The calculation type.</param>
    /// <returns>A short hash string.</returns>
    public static string GenerateDefinitionHash(string calculationType)
    {
        return ComputeHash(calculationType);
    }

    /// <summary>
    /// Generates a prefix for invalidating all entries of a calculation type.
    /// </summary>
    /// <param name="calculationType">The calculation type.</param>
    /// <returns>A prefix string for pattern-based invalidation.</returns>
    public string GenerateTypePrefix(string calculationType)
    {
        var definitionHash = ComputeHash(calculationType);
        return $"{_options.KeyPrefix}{definitionHash}:";
    }

    private static string ComputeHash(params object[] parts)
    {
        var json = JsonSerializer.Serialize(parts);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToBase64String(hashBytes)[..12]
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string ComputeValuesHash(decimal[] values)
    {
        if (values is null || values.Length == 0)
        {
            return "empty";
        }

        var sortedValues = new decimal[values.Length];
        Array.Copy(values, sortedValues, values.Length);
        Array.Sort(sortedValues);

        var json = JsonSerializer.Serialize(sortedValues);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToBase64String(hashBytes)[..12]
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
