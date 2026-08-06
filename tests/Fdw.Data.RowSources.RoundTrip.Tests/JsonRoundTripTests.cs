using System.Collections.Generic;
using System.IO;
using System.Text;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Round-trip tests for the JSON writer + reader: write rows, read them back, assert equal.
/// </summary>
public class JsonRoundTripTests
{
    private static List<Dictionary<string, object?>> SampleRows() =>
    [
        new() { ["id"] = 1L, ["name"] = "Alice", ["active"] = true },
        new() { ["id"] = 2L, ["name"] = "Bob", ["active"] = false },
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
        using (var writer = new JsonStreamRowWriter(tw))
        {
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        // Act — read back (root array)
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = new JsonStreamRowSource(stream);
        var readBack = Drain(reader);

        // Assert
        readBack.Count.ShouldBe(rows.Count);
        readBack[0]["id"].ShouldBe(1L);
        readBack[0]["name"].ShouldBe("Alice");
        readBack[0]["active"].ShouldBe(true);
        readBack[1]["name"].ShouldBe("Bob");
        readBack[1]["active"].ShouldBe(false);
    }

    private static List<Dictionary<string, object?>> Drain(JsonStreamRowSource reader)
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
