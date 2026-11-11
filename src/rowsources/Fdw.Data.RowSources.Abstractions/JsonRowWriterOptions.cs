using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Options for JSON row writing. The write-side mirror of <see cref="JsonRowSourceOptions"/>;
/// every knob maps 1:1 to a <see cref="System.Text.Json.JsonWriterOptions"/> /
/// <see cref="System.Text.Json.Utf8JsonWriter"/> setting.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class JsonRowWriterOptions : RowWriterOptions
{
    /// <summary>
    /// Gets or sets whether the output JSON is indented (System.Text.Json <c>Indented</c>).
    /// </summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// Gets or sets whether to skip validation of the written structure
    /// (System.Text.Json <c>SkipValidation</c>).
    /// </summary>
    public required bool SkipValidation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether each record is written as its own line (JSON Lines) rather than as an element of one array
    /// (FDW-4971). Set by the format type that builds the options; lines are never indented.
    /// </summary>
    public required bool RecordsAreLines { get; set; }
}
