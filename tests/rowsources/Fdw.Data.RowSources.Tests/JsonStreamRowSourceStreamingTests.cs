using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the JSON reader streaming record by record (FDW-4971): the document is read as the stream is read, never whole; a top-level
/// array, an array at the records path and JSON Lines all stream; a malformed record ends the read with its own code after the records
/// before it.
/// </summary>
public sealed class JsonStreamRowSourceStreamingTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AJsonArrayFarLargerThanTheBufferStreamsEveryRecordAndIsNotReadAheadWhole()
    {
        const int records = 50_000;
        var bytes = Encoding.UTF8.GetBytes("[" + string.Join(",", Enumerable.Range(0, records).Select(i => $$"""{"id":{{i}},"name":"n{{i}}"}""")) + "]");
        using var stream = new CountingStream(bytes);
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 1024));

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        stream.BytesRead.ShouldBeLessThan(bytes.Length / 100);
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(0L);

        var count = 1;
        long last = 0;
        while (await source.Read(TestContext.Current.CancellationToken))
        {
            count++;
            last = (long)source.GetValue(source.GetFieldOrdinal("id"))!;
        }

        count.ShouldBe(records);
        last.ShouldBe(records - 1);
        source.ReadFailure.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnArrayAtTheRecordsPathStreamsPastALargeValueItSkips()
    {
        var skipped = string.Join(",", Enumerable.Range(0, 5_000).Select(i => $$"""{"x":{{i}}}"""));
        var json = $$$"""{"meta":{"note":"before","big":[{{{skipped}}}]},"data":{"items":[{"id":1},{"id":2},{"id":3}]}}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 64, path: "$.data.items"));

        var ids = new List<long>();
        while (await source.Read(TestContext.Current.CancellationToken))
            ids.Add((long)source.GetValue(source.GetFieldOrdinal("id"))!);

        ids.ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task JsonLinesStreamEachLineAsARecord()
    {
        var json = string.Join("\n", Enumerable.Range(0, 1_000).Select(i => $$"""{"id":{{i}}}""")) + "\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 128, lines: true));

        var count = 0;
        while (await source.Read(TestContext.Current.CancellationToken))
        {
            source.GetValue(source.GetFieldOrdinal("id")).ShouldBe((long)count);
            count++;
        }

        count.ShouldBe(1_000);
        source.ReadFailure.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordLargerThanTheBufferIsReadWhole()
    {
        var big = new string('x', 5_000);
        var json = $$"""[{"id":1,"text":"{{big}}"},{"id":2,"text":"short"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 16));

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        ((string)source.GetValue(source.GetFieldOrdinal("text"))!).Length.ShouldBe(5_000);
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMalformedRecordMidFileEndsTheReadWithItsCodeAfterTheRecordsBeforeIt()
    {
        var json = """[{"id":1},{"id":2},{"id": ,},{"id":4}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 8));

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();

        var failure = source.ReadFailure.ShouldNotBeNull();
        failure.Code.ShouldBe("ABSTRACTIONS2-21000");
        failure.Message.ShouldContain("JSON record 3 is malformed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACursorRecordSourceHandsOutTheMalformedRecordAsItsLastFailedElement()
    {
        var json = """[{"id":1},{"id":2},{"id": ,}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var records = new CursorRecordSource(new JsonStreamRowSource(stream, Options(bufferSize: 32)), [Mock.Of<IDataField>(f => f.Name == "id")]);

        var results = new List<Fdw.Results.IGenericResult<DataRecord>>();
        await foreach (var record in records.Read(TestContext.Current.CancellationToken))
            results.Add(record);

        results.Count.ShouldBe(3);
        results.Take(2).ShouldAllBe(r => r.IsSuccess);
        results[2].IsFailure.ShouldBeTrue();
        results[2].CurrentMessage.ShouldNotBeNull().ShouldContain("JSON record 3 is malformed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheWriterInLinesModeWritesOneRecordPerLineThatTheLinesReaderReadsBack()
    {
        var target = new StringWriter();
        using (var writer = new JsonStreamRowWriter(target, new JsonRowWriterOptions { RecordsAreLines = true, SkipValidation = false, Columns = ["id"] }))
        {
            writer.Write(new Dictionary<string, object?> { ["id"] = 1 });
            writer.Write(new Dictionary<string, object?> { ["id"] = 2 });
            writer.Flush();
        }

        var text = target.ToString();
        text.ShouldBe("{\"id\":1}\n{\"id\":2}\n");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var source = new JsonStreamRowSource(stream, Options(bufferSize: 64, lines: true));
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    private static JsonRowSourceOptions Options(int bufferSize, string? path = null, bool lines = false) => new()
    {
        RecordsAreLines = lines,
        RowArrayPath = path,
        ContinueOnError = true,
        PropertyNameCaseInsensitive = true,
        FlattenNestedObjects = false,
        FlattenSeparator = ".",
        BufferSize = bufferSize,
        MaxDepth = 64,
        MaxStringLength = 128 * 1024 * 1024,
        AllowTrailingCommas = true,
    };

    // A stream that counts what was read from it, so a test sees how far ahead of the records the reader went.
    private sealed class CountingStream(byte[] content) : MemoryStream(content)
    {
        public long BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }
    }
}
