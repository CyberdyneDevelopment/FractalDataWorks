using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Options for JSON row source processing.
/// </summary>
public sealed class JsonRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the maximum JSON nesting depth.
    /// Default is 64.
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// Gets or sets the maximum string length for individual values.
    /// Default is 128MB.
    /// </summary>
    public int MaxStringLength { get; set; } = 128 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the JSON path to the array containing row objects.
    /// Example: "$.data.items" or "data.items" ($ prefix optional)
    /// If null, expects root-level array.
    /// </summary>
    public string? RowArrayPath { get; set; }

    /// <summary>
    /// Gets or sets whether to allow trailing commas in JSON.
    /// Default is true.
    /// </summary>
    public bool AllowTrailingCommas { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow comments in JSON.
    /// Default is false.
    /// </summary>
    public bool AllowComments { get; set; }

    /// <summary>
    /// Gets or sets whether to use case-insensitive property matching.
    /// Default is true.
    /// </summary>
    public bool PropertyNameCaseInsensitive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to flatten nested objects into dot-notation fields.
    /// Example: { "address": { "city": "NY" } } becomes "address.city": "NY"
    /// Default is false.
    /// </summary>
    public bool FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator for flattened field names.
    /// Default is "." (dot).
    /// </summary>
    public string FlattenSeparator { get; set; } = ".";
}
