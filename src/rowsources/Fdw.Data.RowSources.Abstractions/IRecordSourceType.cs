using System.IO;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// TypeOption interface for record source types (DataReader, Xml, Json, Delimited, FixedWidth, Http).
/// The factory seam: <c>RecordSourceTypes.ByName(format).Create(context)</c> builds a reader from a
/// container's configuration without the caller ever naming a concrete reader type.
/// </summary>
/// <remarks>
/// Record source types are TypeCollection members, not ServiceTypes, because they don't require DI
/// registration — they're lightweight adapters around existing stream/reader instances.
/// <para>
/// Why renamed from <c>IRowSourceType</c>: these build RECORD sources (items or rows), of which a row
/// source is one specialization. Json/Xml types build item sources (<see cref="IRecordSource{T}"/>);
/// Delimited/FixedWidth/DataReader types build row sources (<see cref="IRowSource"/>).
/// </para>
/// </remarks>
public interface IRecordSourceType : ITypeOption<int, RecordSourceTypeBase>
{
    /// <summary>
    /// Builds a record source from the container's configuration (content + field schema + format options).
    /// This is the config-driven factory surface: the format name selected the type; this method consumes
    /// the rest of the configuration dynamically.
    /// </summary>
    /// <param name="context">
    /// The container configuration: content stream, field children (flyweight schema), and format options.
    /// </param>
    /// <returns>
    /// A record source over the content. Json/Xml return an item source; Delimited/FixedWidth/Tabular
    /// return an <see cref="IRowSource"/> (which is an <c>IRecordSource&lt;DataRecord&gt;</c>).
    /// </returns>
    IRecordSource<DataRecord> Create(RecordSourceContext context);

    /// <summary>
    /// Creates a format-specific row cursor/reader over the supplied content stream.
    /// </summary>
    /// <param name="content">The input stream containing data in this type's format.</param>
    /// <param name="options">
    /// Format-specific options (the concrete subclass of <see cref="RowSourceOptions"/> for this
    /// format, e.g. <c>JsonRowSourceOptions</c>), built by <see cref="CreateOptions"/>. Required: a reader takes no value of its own.
    /// </param>
    /// <returns>A cursor/reader positioned before the first row.</returns>
    /// <remarks>
    /// Why retained alongside <see cref="Create(RecordSourceContext)"/>: existing consumers resolve the
    /// type via <c>RecordSourceTypes.ByName(container.Format.Name)</c> and read through the low-level
    /// cursor (<see cref="IRowSourceReader"/>). The new <see cref="Create(RecordSourceContext)"/> overload
    /// is the record-source surface layered on the same parsing; this cursor surface stays so the current
    /// HTTP read path keeps working.
    /// </remarks>
    IRowSourceReader CreateReader(Stream content, RowSourceOptions options);

    /// <summary>
    /// Builds this format's own reader options for a container: each choice from what the container's metadata states, each tuning value
    /// from this format's own declaration.
    /// </summary>
    /// <param name="container">The container whose metadata and fields state the format's choices.</param>
    /// <returns>Options of the concrete type this format's reader expects, or the failure naming the first choice the container leaves out.</returns>
    /// <remarks>
    /// Why the format builds them (FDW-3823): the HTTP protocols and the file-system and HTTP record connectors each read the container's
    /// metadata with their own copy of this mapping, and each held literal defaults. A choice (a separator, a header, an element name) is
    /// never defaulted: a container that leaves one out is refused, naming it.
    /// </remarks>
    IGenericResult<RowSourceOptions> CreateOptions(IStorageContainer container);

    /// <summary>Gets the buffer size, in bytes, this format's reader streams with. Declared by the format; a host may change it at registration.</summary>
    int BufferSize { get; set; }

    /// <summary>
    /// Gets whether this source type supports synchronous reading.
    /// </summary>
    bool SupportsSync { get; }

    /// <summary>
    /// Gets whether this source type supports asynchronous reading.
    /// </summary>
    bool SupportsAsync { get; }

    /// <summary>
    /// Gets whether this source type supports reset/replay.
    /// </summary>
    bool SupportsReset { get; }

    /// <summary>
    /// Gets the typical allocation overhead per row (0 = zero-alloc capable).
    /// </summary>
    int TypicalAllocationsPerRow { get; }

    /// <summary>
    /// Gets the data format this source handles (Tabular, Json, Xml).
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets a value indicating whether this format's file is bytes, not text (an Excel workbook is a zip).
    /// </summary>
    /// <remarks>
    /// The read-side counterpart of <see cref="IRecordWriterType.WritesBinary"/>. A caller that holds a file's content
    /// asks this before choosing how to read it: a text format is decoded and handed over as text, a binary one must
    /// reach the reader as the file's own bytes.
    /// </remarks>
    bool ReadsBinary { get; }

    /// <summary>
    /// Gets a value indicating whether this format parses its records as its content stream is read, holding about one record at a time
    /// (FDW-4967).
    /// </summary>
    /// <remarks>
    /// A caller that reads a file of any size asks this before choosing how to read it: an incremental format is read from a stream as it
    /// is parsed; any other is read whole, and the caller says so.
    /// </remarks>
    bool ParsesIncrementally { get; }
}
