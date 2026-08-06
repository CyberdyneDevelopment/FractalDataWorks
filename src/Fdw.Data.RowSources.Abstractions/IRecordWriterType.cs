using System.IO;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// TypeOption interface for record writer types (Json, Xml, Delimited, FixedWidth). The write-side
/// mirror of <see cref="IRecordSourceType"/>: <c>RecordWriterTypes.ByName(format).Create(context)</c>
/// builds a writer from a container's configuration.
/// </summary>
/// <remarks>
/// Record writer types are TypeCollection members, not ServiceTypes, because they don't require DI
/// registration — they're lightweight adapters around a target <see cref="TextWriter"/>. Selection is
/// format-driven.
/// <para>
/// Why renamed from <c>IRowWriterType</c>: these build RECORD writers (item or row). Json/Xml build item
/// writers (<see cref="IRecordWriter{T}"/>); Delimited/FixedWidth build row writers (<see cref="IRowWriter"/>).
/// </para>
/// </remarks>
public interface IRecordWriterType : ITypeOption<int, RecordWriterTypeBase>
{
    /// <summary>
    /// Gets the data format this writer handles (Json, Xml, Delimited, FixedWidth).
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Builds a record writer from the container's configuration (target + field schema + format options).
    /// The config-driven factory surface.
    /// </summary>
    /// <param name="context">The container configuration: target, field children, and format options.</param>
    /// <returns>
    /// A record writer over the target. Json/Xml return an item writer; Delimited/FixedWidth return an
    /// <see cref="IRowWriter"/> (which is an <c>IRecordWriter&lt;DataRecord&gt;</c>).
    /// </returns>
    IRecordWriter<DataRecord> Create(RecordWriterContext context);

    /// <summary>
    /// Creates a format-specific row writer over the supplied target.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="options">
    /// Format-specific options (the concrete subclass of <see cref="RowWriterOptions"/> for this
    /// format). Null requests the format's defaults.
    /// </param>
    /// <returns>A record writer ready to accept records.</returns>
    /// <remarks>
    /// Why retained alongside <see cref="Create(RecordWriterContext)"/>: existing consumers build a writer
    /// from a target + options. The new overload is the config-driven surface layered over the same writers.
    /// </remarks>
    IRecordWriter<DataRecord> CreateWriter(TextWriter target, RowWriterOptions? options);
}
