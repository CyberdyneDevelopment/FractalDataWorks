using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Options for JSON row source processing.
/// </summary>
public sealed class JsonRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the maximum JSON nesting depth.
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the maximum string length for individual values.
    /// </summary>
    public int MaxStringLength { get; set; }

    /// <summary>
    /// Gets or sets the JSON path to the array containing row objects.
    /// Example: "$.data.items" or "data.items" ($ prefix optional)
    /// If null, expects root-level array.
    /// </summary>
    public string? RowArrayPath { get; set; }

    /// <summary>
    /// Gets or sets whether to allow trailing commas in JSON.
    /// </summary>
    public bool AllowTrailingCommas { get; set; }

    /// <summary>
    /// Gets or sets whether to allow comments in JSON.
    /// </summary>
    public bool AllowComments { get; set; }

    /// <summary>
    /// Gets or sets whether to use case-insensitive property matching.
    /// </summary>
    public required bool PropertyNameCaseInsensitive { get; set; }

    /// <summary>
    /// Gets or sets whether to flatten nested objects into dot-notation fields.
    /// Example: { "address": { "city": "NY" } } becomes "address.city": "NY"
    /// </summary>
    public required bool FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator for flattened field names.
    /// </summary>
    public required string FlattenSeparator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether each top-level value of the document is a record (JSON Lines) rather than the objects of one
    /// array (FDW-4971). Set by the format type that builds the options; the records path does not apply to lines.
    /// </summary>
    public required bool RecordsAreLines { get; set; }
}
