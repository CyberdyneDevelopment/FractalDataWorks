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
    /// Default is false.
    /// </summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// Gets or sets whether to skip validation of the written structure
    /// (System.Text.Json <c>SkipValidation</c>). Default is false.
    /// </summary>
    public bool SkipValidation { get; set; }
}
