using System;
using System.Collections.Generic;
using Fdw.Data.Transformations;

namespace Fdw.Data.Transformations.Tests;

/// <summary>Builds a <see cref="TransformationContext"/> for a test.</summary>
internal static class TransformTestContext
{
    /// <summary>A context carrying the given parameters and no row.</summary>
    public static TransformationContext With(params (string Key, string Value)[] parameters) =>
        With(row: null, parameters);

    /// <summary>A context carrying the given parameters and a row the transformer can read.</summary>
    public static TransformationContext With(
        IReadOnlyDictionary<string, object?>? row,
        params (string Key, string Value)[] parameters)
    {
        var bag = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
        {
            bag[key] = value;
        }

        return new TransformationContext
        {
            Parameters = bag,
            CurrentRecord = row ?? new Dictionary<string, object?>(StringComparer.Ordinal),
        };
    }

    /// <summary>A row, written the way a source would hand one over.</summary>
    public static IReadOnlyDictionary<string, object?> Row(params (string Key, object? Value)[] fields)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in fields)
        {
            row[key] = value;
        }

        return row;
    }
}
