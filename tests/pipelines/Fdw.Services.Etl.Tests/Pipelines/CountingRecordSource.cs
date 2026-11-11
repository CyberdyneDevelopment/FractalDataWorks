using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A gateway's stable read, faked for pipeline tests (FDW-4911): hands out rows as records, counts how many the reader pulled, and records
/// "closed after N" in the shared event list when it is disposed, so a test sees that a read stopped early and that it was released.
/// </summary>
internal sealed class CountingRecordSource : IRecordSource<DataRecord>
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;
    private readonly List<string> _events;
    private int _pulled;

    /// <summary>Initializes a new instance of the <see cref="CountingRecordSource"/> class.</summary>
    /// <param name="rows">The rows; the first row's fields are the schema.</param>
    /// <param name="events">Receives "closed after N" when the source is disposed.</param>
    public CountingRecordSource(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, List<string> events)
    {
        _rows = rows;
        _events = events;
        Schema = SchemaOf(rows.Count == 0 ? [] : rows[0].Keys);
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <summary>Builds a schema of the named fields.</summary>
    /// <param name="names">The field names, in order.</param>
    /// <returns>The schema.</returns>
    public static RecordSchema SchemaOf(IEnumerable<string> names) =>
        new([.. names.Select((name, ordinal) => Mock.Of<IDataField>(field => field.Name == name && field.Ordinal == ordinal))]);

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in _rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _pulled++;
            yield return GenericResult<DataRecord>.Success(RowListRecordSource.Record(Schema, row));
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _events.Add($"closed after {_pulled}");

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
