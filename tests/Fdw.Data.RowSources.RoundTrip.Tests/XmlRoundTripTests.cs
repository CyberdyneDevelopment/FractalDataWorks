using System.Collections.Generic;
using System.IO;
using System.Text;
using Fdw.Data.RowSources.Xml.Abstractions;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Round-trip tests for the XML writer + reader: write rows, read them back, assert equal.
/// </summary>
public class XmlRoundTripTests
{
    private static List<Dictionary<string, object?>> SampleRows() =>
    [
        new() { ["Id"] = "1", ["Name"] = "Alice", ["City"] = "NYC" },
        new() { ["Id"] = "2", ["Name"] = "Bob", ["City"] = "LA" },
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
        using (var writer = new XmlStreamRowWriter(tw, new XmlRowWriterOptions
        {
            RootElementName = "Rows",
            RowElementName = "Row"
        }))
        {
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        // Act — read back
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = new XmlStreamRowSource(stream, new XmlRowSourceOptions
        {
            RowElementName = "Row",
            UseElementContent = true,
            IncludeAttributes = false
        });

        var readBack = Drain(reader);

        // Assert
        readBack.Count.ShouldBe(rows.Count);
        readBack[0]["Id"].ShouldBe("1");
        readBack[0]["Name"].ShouldBe("Alice");
        readBack[0]["City"].ShouldBe("NYC");
        readBack[1]["Name"].ShouldBe("Bob");
    }

    private static List<Dictionary<string, object?>> Drain(XmlStreamRowSource reader)
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
