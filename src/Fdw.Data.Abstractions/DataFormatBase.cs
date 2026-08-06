using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data format definitions.
/// </summary>
/// <remarks>
/// Data formats represent different ways data can be structured and transmitted.
/// Inherit from this class to define specific formats like SQL, JSON, XML, CSV, etc.
/// </remarks>
public abstract class DataFormatBase : TypeOptionBase<int, DataFormatBase>, IDataFormat
{
    /// <inheritdoc/>
    public bool SupportsSchemaDiscovery { get; }

    /// <inheritdoc/>
    public bool SupportsStreaming { get; }

    /// <inheritdoc/>
    public string MimeType { get; }

    /// <inheritdoc/>
    public string[] FileExtensions { get; }

    /// <inheritdoc/>
    public bool IsBinary { get; }

    /// <inheritdoc/>
    public bool SupportsCompression { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFormatBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this format.</param>
    /// <param name="name">The name of this format.</param>
    /// <param name="mimeType">The MIME type for this format.</param>
    /// <param name="fileExtensions">The file extensions for this format.</param>
    /// <param name="isBinary">Whether this format is binary.</param>
    /// <param name="supportsSchemaDiscovery">Whether this format supports schema discovery.</param>
    /// <param name="supportsStreaming">Whether this format supports streaming.</param>
    /// <param name="supportsCompression">Whether this format supports compression.</param>
    protected DataFormatBase(
        int id,
        string name,
        string mimeType,
        string[] fileExtensions,
        bool isBinary,
        bool supportsSchemaDiscovery,
        bool supportsStreaming,
        bool supportsCompression) : base(id, name)
    {
        MimeType = mimeType;
        FileExtensions = fileExtensions;
        IsBinary = isBinary;
        SupportsSchemaDiscovery = supportsSchemaDiscovery;
        SupportsStreaming = supportsStreaming;
        SupportsCompression = supportsCompression;
    }
}