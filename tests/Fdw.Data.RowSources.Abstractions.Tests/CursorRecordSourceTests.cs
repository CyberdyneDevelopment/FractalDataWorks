using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class CursorRecordSourceTests
{
    private static IReadOnlyList<IDataField> Fields(params string[] names)
    {
        var list = new List<IDataField>(names.Length);
        foreach (var name in names)
        {
            var f = new Mock<IDataField>();
            f.Setup(x => x.Name).Returns(name);
            list.Add(f.Object);
        }

        return list;
    }

    private static async Task<List<T>> ReadAllAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
            list.Add(item);
        return list;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaProjectsContainerFields()
    {
        using var sut = new CursorRecordSource(new FakeCursor([], "Id", "Name"), Fields("Id", "Name"));

        sut.Schema.FieldCount.ShouldBe(2);
        sut.Schema.GetFieldOrdinal("Name").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadProjectsEachCursorPositionIntoDataRecord()
    {
        var cursor = new FakeCursor(
            [[1L, "Alice"], [2L, "Bob"]],
            "Id", "Name");
        using var sut = new CursorRecordSource(cursor, Fields("Id", "Name"));

        var records = await ReadAllAsync(sut.Read(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

        records.Count.ShouldBe(2);
        records[0].IsSuccess.ShouldBeTrue();
        records[0].Value["Name"].ShouldBe("Alice");
        records[1].Value["Id"].ShouldBe(2L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RecordsShareTheSameFlyweightSchemaInstance()
    {
        var cursor = new FakeCursor([[1L, "Alice"], [2L, "Bob"]], "Id", "Name");
        using var sut = new CursorRecordSource(cursor, Fields("Id", "Name"));

        var records = await ReadAllAsync(sut.Read(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

        ReferenceEquals(records[0].Value.Schema, sut.Schema).ShouldBeTrue();
        ReferenceEquals(records[1].Value.Schema, sut.Schema).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadAsyncProjectsEachCursorPosition()
    {
        var cursor = new FakeCursor([[1L, "Alice"]], "Id", "Name");
        await using var sut = new CursorRecordSource(cursor, Fields("Id", "Name"));

        var records = new List<DataRecord>();
        await foreach (var r in sut.Read(TestContext.Current.CancellationToken))
        {
            records.Add(r.Value);
        }

        records.Count.ShouldBe(1);
        records[0]["Name"].ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RowCursorRecordSourceIsRowSourceAndExposesCursor()
    {
        var cursor = new FakeCursor([], "Id");
        using var sut = new RowCursorRecordSource(cursor, Fields("Id"));

        sut.ShouldBeAssignableTo<IRowSource>();
        sut.ShouldBeAssignableTo<IRecordSource<DataRecord>>();
        ReferenceEquals(sut.Cursor, cursor).ShouldBeTrue();
    }

    private sealed class FakeCursor : IRowSourceReader
    {
        private readonly IReadOnlyList<object?[]> _rows;
        private readonly string[] _names;
        private int _index = -1;

        public FakeCursor(IReadOnlyList<object?[]> rows, params string[] names)
        {
            _rows = rows;
            _names = names;
        }

        public bool HasCurrentRow => _index >= 0 && _index < _rows.Count;
        public int FieldCount => _names.Length;
        public bool CanReset => false;
        public int EstimatedAllocationsPerRow => 1;

        public bool Read()
        {
            _index++;
            return _index < _rows.Count;
        }

        public void Reset() => _index = -1;

        public string GetFieldName(int ordinal) => _names[ordinal];

        public int GetFieldOrdinal(string fieldName)
        {
            for (var i = 0; i < _names.Length; i++)
            {
                if (string.Equals(_names[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsNull(int ordinal) => _rows[_index][ordinal] is null;

        public object? GetValue(int ordinal) => _rows[_index][ordinal];

        public object? GetConvertedValue(int ordinal, IDataTypeConverter converter) => GetValue(ordinal);

        public void Dispose()
        {
        }
    }
}
