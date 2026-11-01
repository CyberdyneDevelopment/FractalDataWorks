using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>Writes records in a binary format into a byte array: the generic, format-driven path a connection takes for such a format.</summary>
/// <remarks>
/// A connection that writes text builds a <see cref="TextWriter"/> over a buffer and hands the string to its client. A binary
/// format has no string form, so this builds the writer through <see cref="IRecordWriterType.Create"/> over a byte stream,
/// lets the caller write, and finishes the file by disposing the writer. The connection then hands the bytes to its client.
/// The file is held in memory whole: a connection's write already takes every row in a list.
/// </remarks>
public static class RecordWriterBytes
{
    /// <summary>Writes records through a binary format and returns the finished file.</summary>
    /// <param name="format">The format; it must report <see cref="IRecordWriterType.WritesBinary"/>.</param>
    /// <param name="fields">The container's field children.</param>
    /// <param name="options">Format-specific options; null requests the format's defaults.</param>
    /// <param name="write">Writes the rows into the writer it is given.</param>
    /// <returns>The file's bytes.</returns>
    /// <exception cref="NotSupportedException">The format is a text format.</exception>
    public static byte[] Write(
        IRecordWriterType format, IReadOnlyList<IDataField> fields, RowWriterOptions? options, Action<IRecordWriter<DataRecord>> write)
    {
        if (format is null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        if (write is null)
        {
            throw new ArgumentNullException(nameof(write));
        }

        if (!format.WritesBinary)
        {
            throw new NotSupportedException($"The {format.Format} format is written as text; it has no byte form.");
        }

        using var buffer = new MemoryStream();
        using (var writer = format.Create(RecordWriterContext.ForBytes(buffer, fields, options)))
        {
            write(writer);
        }

        return buffer.ToArray();
    }
}
