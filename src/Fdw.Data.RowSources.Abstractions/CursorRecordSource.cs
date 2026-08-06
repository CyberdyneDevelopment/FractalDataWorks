using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// The shared record-source adapter that turns a low-level <see cref="IRowSourceReader"/> cursor into an
/// <see cref="IRecordSource{T}"/> of <see cref="DataRecord"/>: it advances the cursor and projects each
/// position into a <see cref="DataRecord"/> over a shared <see cref="RecordSchema"/> flyweight.
/// </summary>
/// <remarks>
/// This is the one place cursor→record projection lives, so every format reuses it: Xml (an item source
/// with a genuinely non-tabular document shape) constructs a <see cref="CursorRecordSource"/> directly
/// (does NOT expose the cursor), while row formats (Delimited/FixedWidth) AND Json wrap it in
/// <see cref="RowCursorRecordSource"/> (which additionally exposes the <see cref="IRowSource.Cursor"/>).
/// Json is not "genuinely tabular" the way Delimited/FixedWidth are — its <c>Read()</c> enumeration still
/// yields <see cref="DataRecord"/>s schema-projected via <see cref="Project"/> below — but its cursor
/// (<see cref="Fdw.Data.RowSources.Json.Abstractions.JsonStreamRowSource"/>) tracks every property actually present in the source object, a
/// possible superset of the declared schema, so exposing <see cref="IRowSource.Cursor"/> lets a caller
/// (e.g. the FileSystem connection's config write path) read the FULL row when it needs to preserve
/// columns beyond the declared schema. The flyweight schema comes from the container's field children;
/// values are read by ordinal from the cursor for each record.
/// <para>
/// Why per-record <c>object?[]</c>: the value buffer must outlive the cursor position (the consumer may
/// hold the record), so each yielded record owns its values array; the <em>schema</em> is shared. The
/// <see cref="DataRecord.Values"/> span gives zero-copy windowed reads over that array. A pooled/columnar
/// bulk buffer is a later performance refinement on top of this correct baseline.
/// </para>
/// </remarks>
public class CursorRecordSource : IRecordSource<DataRecord>
{
    private readonly IRowSourceReader _reader;
    private readonly IAsyncRowSourceReader? _asyncReader;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorRecordSource"/> class.
    /// </summary>
    /// <param name="reader">The low-level cursor/reader to project records from.</param>
    /// <param name="fields">The container's field children — the flyweight schema for produced records.</param>
    /// <param name="logger">Logger for record-source diagnostics; falls back to <see cref="NullLogger.Instance"/> when null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> or <paramref name="fields"/> is null.</exception>
    public CursorRecordSource(IRowSourceReader reader, IReadOnlyList<IDataField> fields, ILogger? logger = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Schema = new RecordSchema(fields ?? throw new ArgumentNullException(nameof(fields)));
        _asyncReader = reader as IAsyncRowSourceReader;
        // Why: NullLogger keeps the record source functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
        // Why: one Debug line at construction records the schema shape; per-record logging is forbidden on this
        // hot path (the projection in Project() is per-row and must stay allocation/log free).
        RowSourceLog.RecordSourceCreated(_logger, GetType().Name, Schema.Fields.Count);
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <summary>
    /// Gets the underlying cursor. Exposed to <see cref="RowCursorRecordSource"/> so a row source can
    /// surface it through <see cref="IRowSource.Cursor"/> without re-reading.
    /// </summary>
    protected IRowSourceReader Reader => _reader;

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        while (_reader.Read())
        {
            yield return Project();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_asyncReader is not null)
        {
            while (await _asyncReader.Read(cancellationToken).ConfigureAwait(false))
            {
                yield return Project();
            }

            yield break;
        }

        // Why: the reader is sync-only — drive the sync cursor but still honor cancellation between
        // records. No blocking-over-async sin (there is no async work to block on).
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_reader.Read())
            {
                yield break;
            }

            yield return Project();
        }
    }

    // Why: build the record's value array by reading every schema field's value from the cursor by its
    // own ordinal (the cursor's ordinal for that name), so the record's positions align to the flyweight
    // schema regardless of the cursor's internal column order. A field absent from the cursor reads null.
    private IGenericResult<DataRecord> Project()
    {
        var fields = Schema.Fields;
        var values = new object?[fields.Count];
        for (var i = 0; i < fields.Count; i++)
        {
            var ordinal = _reader.GetFieldOrdinal(fields[i].Name);
            values[i] = ordinal < 0 ? null : _reader.GetValue(ordinal);
        }

        return GenericResult<DataRecord>.Success(new DataRecord(Schema, values));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the underlying cursor.
    /// </summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            _reader.Dispose();
        }
    }

    /// <inheritdoc />
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_asyncReader is not null)
        {
            await _asyncReader.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _reader.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
