using System.Collections.Generic;
using System.IO;
using System.Text;
using Fdw.Data.RowSources.Delimited.Abstractions;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Round-trip tests for the delimited (CSV) writer + reader: write rows, read them back, assert equal.
/// </summary>
public class DelimitedRoundTripTests
{
    private static readonly string[] Columns = ["Id", "Name", "City"];

    private static List<Dictionary<string, object?>> SampleRows() =>
    [
        new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
        new() { ["Id"] = "2", ["Name"] = "Bob, Jr", ["City"] = "LA" },
        new() { ["Id"] = "3", ["Name"] = "Carol \"C\"", ["City"] = "SF" },
    ];

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task WriteThenReadEqualsOriginalWithHeader()
    {
        // Arrange
        var rows = SampleRows();
        var sb = new StringBuilder();

        // Act — write
        using (var tw = new StringWriter(sb))
        using (var writer = new DelimitedStreamRowWriter(tw, new DelimitedRowWriterOptions
        {
            Columns = [.. Columns],
            WriteHeader = true,
            Separator = ","
        }))
        {
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        // Act — read back
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = new DelimitedStreamRowSource(stream, new DelimitedRowSourceOptions
        {
            Columns = [.. Columns],
            HasHeader = true,
            Separator = ","
        });

        var readBack = Drain(reader);

        // Assert
        readBack.Count.ShouldBe(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            foreach (var col in Columns)
            {
                readBack[i][col].ShouldBe(rows[i][col]);
            }
        }
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public async Task WriteThenReadEqualsOriginalNoHeader()
    {
        var rows = SampleRows();
        var sb = new StringBuilder();

        using (var tw = new StringWriter(sb))
        using (var writer = new DelimitedStreamRowWriter(tw, new DelimitedRowWriterOptions { Columns = [.. Columns] }))
        {
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = new DelimitedStreamRowSource(stream, new DelimitedRowSourceOptions { Columns = [.. Columns] });
        var readBack = Drain(reader);

        readBack.Count.ShouldBe(rows.Count);
        readBack[1]["Name"].ShouldBe("Bob, Jr");
        readBack[2]["Name"].ShouldBe("Carol \"C\"");
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void ReaderFailsLoudWhenNoColumns()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("a,b,c"));
        Should.Throw<System.ArgumentException>(() =>
            new DelimitedStreamRowSource(stream, new DelimitedRowSourceOptions()));
    }

    [Fact]
    [Trait("Category", "DataIntegrity")]
    public void WriterFailsLoudWhenNoColumns()
    {
        using var tw = new StringWriter();
        Should.Throw<System.ArgumentException>(() =>
            new DelimitedStreamRowWriter(tw, new DelimitedRowWriterOptions()));
    }

    private static List<Dictionary<string, object?>> Drain(DelimitedStreamRowSource reader)
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
