using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Xml.Abstractions;

/// <summary>
/// Writes flat name→value rows as XML using <see cref="XmlWriter"/>. The write-side mirror of
/// <see cref="XmlStreamRowSource"/>.
/// </summary>
/// <remarks>
/// Output shape is a single root element (<see cref="XmlRowWriterOptions.RootElementName"/>)
/// containing one row element (<see cref="XmlRowWriterOptions.RowElementName"/>) per row, each with
/// one child element per field — the exact inverse of what <see cref="XmlStreamRowSource"/> reads
/// when configured with the same row element name and <c>UseElementContent = true</c>.
/// </remarks>
public sealed class XmlStreamRowWriter : IRecordWriter<DataRecord>
{
    private readonly XmlWriter _writer;
    private readonly XmlRowWriterOptions _options;
    private bool _rootStarted;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStreamRowWriter"/> class.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="options">XML writer options.</param>
    public XmlStreamRowWriter(TextWriter target, XmlRowWriterOptions? options = null)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        _options = options ?? new XmlRowWriterOptions();
        _writer = XmlWriter.Create(target, new XmlWriterSettings
        {
            Indent = _options.Indent,
            OmitXmlDeclaration = _options.OmitXmlDeclaration,
            CloseOutput = false
        });
    }

    /// <inheritdoc />
    public void Write(IReadOnlyDictionary<string, object?> row)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (!_rootStarted)
        {
            _writer.WriteStartElement(_options.RootElementName);
            _rootStarted = true;
        }

        _writer.WriteStartElement(_options.RowElementName);
        foreach (var kv in row)
        {
            _writer.WriteStartElement(kv.Key);
            if (kv.Value is not null)
            {
                _writer.WriteString(Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
            }

            _writer.WriteEndElement();
        }

        _writer.WriteEndElement();
    }

    /// <inheritdoc />
    public void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        foreach (var row in rows)
        {
            Write(row);
        }
    }

    /// <summary>
    /// Writes all rows in the supplied sequence to the target.
    /// </summary>
    /// <param name="rows">The rows to write as flat name→value maps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all rows have been written.</returns>
    public ValueTask WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(row);
        }

        return default;
    }

    /// <inheritdoc />
    // Why: the typed IRecordWriter<DataRecord> surface projects the record's field-array onto the
    // existing dictionary write path via DataRecord.ToDictionary() — the flyweight schema names the
    // emitted XML elements.
    public void Write(DataRecord record) => Write(record.ToDictionary());

    /// <inheritdoc />
    public void Write(IEnumerable<DataRecord> records)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        foreach (var record in records)
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public async ValueTask Write(IAsyncEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (!_rootStarted)
        {
            _writer.WriteStartElement(_options.RootElementName);
            _rootStarted = true;
        }

        _writer.WriteEndElement();
        _writer.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // Why: XmlWriter.DisposeAsync is not available on netstandard2.0 (the target of this
        // abstractions project); the sync Dispose flushes and releases the writer fully.
        Dispose();
        return default;
    }
}
