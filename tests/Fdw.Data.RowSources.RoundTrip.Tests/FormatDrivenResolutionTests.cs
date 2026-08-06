using System.Collections.Generic;
using System.IO;
using System.Text;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.FixedWidth;
using Fdw.Data.RowSources.Json;
using Fdw.Data.RowSources.Xml;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Tests the FORMAT-DRIVEN factory seam: each row-source / row-writer TypeOption exposes its format
/// name and creates its reader/writer through the type-agnostic <see cref="IRecordSourceType.CreateReader"/>
/// / <see cref="IRecordWriterType.CreateWriter"/> contract — the same seam the un-hardcoded connection
/// dispatch uses (resolve type by <c>container.Format.Name</c>, then call CreateReader).
/// </summary>
/// <remarks>
/// The RecordSourceTypes / RecordWriterTypes TypeOptions use <c>RestrictToCurrentCompilation = true</c>,
/// so they are NOT auto-registered into the frozen collection from a referencing test assembly
/// (same convention as <c>RowSourceTypesTests</c>). These tests therefore construct the type
/// instances directly and verify the factory contract each exposes.
/// </remarks>
public class FormatDrivenResolutionTests
{
    public static IEnumerable<object[]> SourceTypes() =>
    [
        [new JsonRowSourceType(), "Json"],
        [new XmlRowSourceType(), "Xml"],
        [new DelimitedRowSourceType(), "Delimited"],
        [new FixedWidthRowSourceType(), "FixedWidth"],
    ];

    public static IEnumerable<object[]> WriterTypes() =>
    [
        [new JsonRowWriterType(), "Json"],
        [new XmlRowWriterType(), "Xml"],
        [new DelimitedRowWriterType(), "Delimited"],
        [new FixedWidthRowWriterType(), "FixedWidth"],
    ];

    [Theory]
    [MemberData(nameof(SourceTypes))]
    [Trait("Category", "Dispatch")]
    public void RowSourceTypeExposesItsFormatName(IRecordSourceType type, string expectedFormat)
    {
        type.Format.ShouldBe(expectedFormat);
        type.Name.ShouldBe(expectedFormat);
    }

    [Theory]
    [MemberData(nameof(WriterTypes))]
    [Trait("Category", "Dispatch")]
    public void RowWriterTypeExposesItsFormatName(IRecordWriterType type, string expectedFormat)
    {
        type.Format.ShouldBe(expectedFormat);
        type.Name.ShouldBe(expectedFormat);
    }

    [Fact]
    [Trait("Category", "Dispatch")]
    public async Task RoundTripViaFactorySeam()
    {
        // Arrange — resolve writer + reader purely through the IRecordWriterType / IRecordSourceType seam,
        // never touching a concrete reader/writer type.
        string[] columns = ["Id", "Name"];
        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Id"] = "1", ["Name"] = "Alice" },
            new Dictionary<string, object?> { ["Id"] = "2", ["Name"] = "Bob" },
        };

        IRecordWriterType writerType = new DelimitedRowWriterType();
        IRecordSourceType readerType = new DelimitedRowSourceType();

        var sb = new StringBuilder();

        // Act — write via the seam. Delimited is a ROW writer, so the factory's IRecordWriter<DataRecord>
        // is an IRowWriter; cast to reach the schema-agnostic dictionary write path the transport uses.
        using (var tw = new StringWriter(sb))
        {
            using var writer = (IRowWriter)writerType.CreateWriter(tw, new DelimitedRowWriterOptions { Columns = [.. columns] });
            await writer.WriteAll(rows, TestContext.Current.CancellationToken);
            writer.Flush();
        }

        // Act — read via the seam
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        using var reader = readerType.CreateReader(stream, new DelimitedRowSourceOptions { Columns = [.. columns] });

        var readBack = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetFieldName(i)] = reader.GetValue(i);
            }

            readBack.Add(row);
        }

        // Assert
        readBack.Count.ShouldBe(2);
        readBack[0]["Name"].ShouldBe("Alice");
        readBack[1]["Name"].ShouldBe("Bob");
    }
}
