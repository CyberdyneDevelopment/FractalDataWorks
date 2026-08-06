using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for format type definitions.
/// Provides metadata about data serialization formats.
/// </summary>
public abstract class FormatTypeBase : TypeOptionBase<int, FormatTypeBase>, IFormatType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this format type.</param>
    /// <param name="name">The name of this format type.</param>
    /// <param name="displayName">The display name for this format type.</param>
    /// <param name="description">The description of this format type.</param>
    /// <param name="mimeType">The MIME type for this format.</param>
    /// <param name="isBinary">Whether this format is binary.</param>
    /// <param name="supportsStreaming">Whether this format supports streaming.</param>
    /// <param name="canonicalFileExtension">
    /// The canonical file extension (with leading dot, e.g. <c>".json"</c>) used when a container of this
    /// format is addressed as a file; empty string for non-file-addressable formats (e.g. Tabular).
    /// Required — every format option MUST declare it (no silent default), mirroring how the other
    /// metadata (name/mimeType/…) is supplied by ctor arg per the FDW TypeOption convention.
    /// </param>
    /// <param name="category">The category for this format type (defaults to "Format").</param>
    protected FormatTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string mimeType,
        bool isBinary,
        bool supportsStreaming,
        string canonicalFileExtension,
        string? category = null)
        : base(id, name, $"Formats:{name}", displayName, description, category ?? "Format")
    {
        MimeType = mimeType;
        IsBinary = isBinary;
        SupportsStreaming = supportsStreaming;
        CanonicalFileExtension = canonicalFileExtension;
    }

    /// <inheritdoc/>
    public string MimeType { get; }

    /// <inheritdoc/>
    public bool IsBinary { get; }

    /// <inheritdoc/>
    public bool SupportsStreaming { get; }

    /// <inheritdoc/>
    public string CanonicalFileExtension { get; }
}
