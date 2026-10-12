using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for data format handlers that serialize/deserialize data.
/// Implementations should use static methods with object pooling for performance.
/// Examples: TabularFormat, JsonFormat, CsvFormat, XmlFormat, ParquetFormat
/// </summary>
public interface IFormat
{
    /// <summary>
    /// Format name (e.g., "Tabular", "Json", "Csv", "Xml", "Parquet").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// MIME type for this format.
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// Whether this format is binary.
    /// </summary>
    bool IsBinary { get; }

    /// <summary>
    /// Whether this format supports streaming.
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Serialize data to a stream.
    /// </summary>
    /// <param name="output">Output stream.</param>
    /// <param name="data">Data to serialize.</param>
    /// <param name="schema">Container schema for type information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Serialize(
        Stream output,
        object data,
        IContainerSchema schema,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserialize data from a stream.
    /// </summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="input">Input stream.</param>
    /// <param name="schema">Container schema for type information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized data.</returns>
    Task<T?> Deserialize<T>(
        Stream input,
        IContainerSchema schema,
        CancellationToken cancellationToken = default);
}
