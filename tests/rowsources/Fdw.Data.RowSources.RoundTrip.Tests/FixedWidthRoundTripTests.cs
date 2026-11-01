using System.Collections.Generic;
using System.IO;
using System.Text;
using Fdw.Data.RowSources.FixedWidth.Abstractions;
using RecordParser.Builders.Writer;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Round-trip tests for the fixed-width writer + reader: write rows, read them back, assert equal.
/// </summary>
public class FixedWidthRoundTripTests
{
    private static List<FixedWidthField> Fields() =>
    [
        new() { Name = "Id", StartIndex = 0, Length = 5, Padding = Padding.Left, PaddingChar = '0' },
        new() { Name = "Name", StartIndex = 5, Length = 12, Padding = Padding.Right },
        new() { Name = "City", StartIndex = 17, Length = 8, Padding = Padding.Right },
    ];

    private static List<Dictionary<string, object?>> SampleRows() =>
    [
        new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
        new() { ["Id"] = "42", ["Name"] = "Bob", ["City"] = "LA" },
    ];

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task WriteThenReadEqualsOriginal()
    {
        // Arrange
        var rows = SampleRows();
        var sb = new StringBuilder();

        // Act — write
        using (var tw = new StringWriter(sb))
        using (var writer = new FixedWidthStreamRowWriter(tw, new FixedWidthRowWriterOptions { Fields = Fields() }))
        {
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        // Act — read back
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = new FixedWidthStreamRowSource(stream, new FixedWidthRowSourceOptions
        {
            Fields = Fields(),
            Trim = true
        });

        var readBack = Drain(reader);

        // Assert — values equal after padding round trip
        readBack.Count.ShouldBe(rows.Count);
        readBack[0]["Name"].ShouldBe("Alice");
        readBack[0]["City"].ShouldBe("NYC");
        readBack[1]["Name"].ShouldBe("Bob");
        readBack[0]["Id"].ShouldBe("1");
        readBack[1]["Id"].ShouldBe("42");
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void ReaderFailsLoudWhenNoFields()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        Should.Throw<System.ArgumentException>(() =>
            new FixedWidthStreamRowSource(stream, new FixedWidthRowSourceOptions()));
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void WriterFailsLoudWhenNoFields()
    {
        using var tw = new StringWriter();
        Should.Throw<System.ArgumentException>(() =>
            new FixedWidthStreamRowWriter(tw, new FixedWidthRowWriterOptions()));
    }

    private static List<Dictionary<string, object?>> Drain(FixedWidthStreamRowSource reader)
    {
        var result = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetFieldName(i)] = reader.GetValue(i);
            }

            result.Add(row);
        }

        return result;
    }
}
