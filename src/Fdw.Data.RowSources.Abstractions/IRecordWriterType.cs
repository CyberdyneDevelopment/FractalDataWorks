using System.Collections.Generic;
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
    /// Gets the IANA media type this format is served as over HTTP (for example
    /// <c>application/json</c>, <c>text/csv</c>).
    /// </summary>
    /// <remarks>
    /// Why the format declares this rather than a lookup table beside the endpoints: a format knows
    /// what it is. A table somewhere else has to be edited every time a format is added, and a format
    /// whose entry was forgotten becomes unreachable over HTTP with nothing to say so.
    /// </remarks>
    string MediaType { get; }

    /// <summary>
    /// Gets the file extension this format is saved under, without the leading dot (for example <c>csv</c>).
    /// </summary>
    /// <remarks>
    /// Declared by the format for the same reason as <see cref="MediaType"/>: a lookup kept beside the endpoints
    /// has to be edited whenever a format is added, and a forgotten entry names downloads wrongly with nothing
    /// to say so.
    /// </remarks>
    string FileExtension { get; }

    /// <summary>
    /// Builds this format's own writer options for the supplied ordered column names.
    /// </summary>
    /// <param name="columns">The ordered column names the writer emits.</param>
    /// <returns>Options of the concrete type this format's writer expects.</returns>
    /// <remarks>
    /// Why the format builds them rather than the caller: <see cref="CreateWriter"/> casts the options
    /// to its own concrete type, so base-class options arrive as null and the writer falls back to its
    /// defaults — which, for a format that requires a column list, means failing on a caller that did
    /// supply one. Only the format knows which type its writer will accept.
    /// </remarks>
    RowWriterOptions CreateOptions(IReadOnlyList<string> columns);

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
