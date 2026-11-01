using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a format type definition - metadata about data serialization formats.
/// </summary>
/// <remarks>
/// Format types describe how data is serialized/deserialized (Tabular, Json, Csv, Xml, Parquet, etc.).
/// </remarks>
public interface IFormatType : ITypeOption<int>
{
    /// <summary>
    /// Gets the configuration key for this format type value.
    /// </summary>
    string ConfigurationKey { get; }

    /// <summary>
    /// Gets the display name for this format type value.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of this format type value.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the MIME type for this format.
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// Gets whether this format is binary.
    /// </summary>
    bool IsBinary { get; }

    /// <summary>
    /// Gets whether this format supports streaming.
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Gets the canonical file extension (including the leading dot, e.g. <c>".json"</c>) used when a
    /// container of this format is addressed as a FILE. Declared explicitly because the extension is NOT
    /// derivable from the format name (e.g. the <c>Csv</c> format's file extension is <c>".csv"</c>,
    /// not <c>".csv"</c>-from-name coincidence, and a <c>Delimited</c> record source is also <c>".csv"</c>).
    /// Empty string means this format is NOT file-addressable (e.g. <c>Tabular</c> = SQL result sets); a
    /// FileSystem-store builder MUST fail loud rather than compose a bare, extension-less file path.
    /// </summary>
    string CanonicalFileExtension { get; }

    /// <summary>
    /// Gets the family of this format: the NAME of the record source and record writer that read and write it
    /// (<c>RecordSourceTypes</c>, <c>RecordWriterTypes</c>). Several formats can share one family: <c>Csv</c> and <c>Tsv</c> are
    /// formats of the <c>Delimited</c> family, which is also a format of its own for any other separator. Every other format
    /// is its own family.
    /// </summary>
    string Family { get; }

    /// <summary>
    /// Gets the field separator this format defines, or <see langword="null"/> when the format does not define one
    /// (the container then states it, as for the plain <c>Delimited</c> format). <c>Csv</c> is <c>","</c>, <c>Tsv</c> is a tab.
    /// </summary>
    string? Separator { get; }
}
