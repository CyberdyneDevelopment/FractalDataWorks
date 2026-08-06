using System.IO;
using Fdw.Collections;

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
    /// format, e.g. <c>JsonRowSourceOptions</c>). Null requests the format's defaults.
    /// </param>
    /// <returns>A cursor/reader positioned before the first row.</returns>
    /// <remarks>
    /// Why retained alongside <see cref="Create(RecordSourceContext)"/>: existing consumers resolve the
    /// type via <c>RecordSourceTypes.ByName(container.Format.Name)</c> and read through the low-level
    /// cursor (<see cref="IRowSourceReader"/>). The new <see cref="Create(RecordSourceContext)"/> overload
    /// is the record-source surface layered on the same parsing; this cursor surface stays so the current
    /// HTTP read path keeps working.
    /// </remarks>
    IRowSourceReader CreateReader(Stream content, RowSourceOptions? options);

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
}
