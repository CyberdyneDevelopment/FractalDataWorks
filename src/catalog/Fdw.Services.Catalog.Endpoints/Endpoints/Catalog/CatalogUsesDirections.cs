using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Which way a uses question reads the stored settings (FDW-1683).</summary>
public static class CatalogUsesDirections
{
    /// <summary>What uses the thing: the pipelines that read it, the terms that explain it.</summary>
    public const string UsedBy = "usedBy";

    /// <summary>What the thing uses: the data sets a pipeline reads and writes.</summary>
    public const string Uses = "uses";

    /// <summary>Gets both directions.</summary>
    public static IReadOnlyList<string> All { get; } = [UsedBy, Uses];

    /// <summary>Reads a direction without regard to case.</summary>
    /// <param name="text">What the request said.</param>
    /// <returns>The direction as this class names it, or null when the text names neither.</returns>
    public static string? Of(string? text) => All.FirstOrDefault(direction => string.Equals(direction, text, StringComparison.OrdinalIgnoreCase));
}
