using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// What a record value's serialized form IS, for the types a real dataset actually carries.
/// </summary>
/// <remarks>
/// Every writer used to convert an unrecognised value with <c>Convert.ToString</c>, which does not
/// fail — it produces something. For <see cref="byte"/>[] that something was the literal text
/// "System.Byte[]", so an export lost the data and still looked successful. These tests exist
/// because that is invisible in the output format and only shows up in the data.
/// </remarks>
public class RecordValueSerializationTests
{
    private static readonly DateTime Timestamp =
        new(2026, 9, 22, 13, 45, 30, DateTimeKind.Utc);

    private static string WriteJson(IReadOnlyDictionary<string, object?> row)
    {
        var text = new StringWriter();
        using (var writer = new JsonStreamRowWriter(text))
        {
            writer.Write(row);
            writer.Flush();
        }

        return text.ToString();
    }

    private static string WriteCsv(IReadOnlyDictionary<string, object?> row, params string[] columns)
    {
        var text = new StringWriter();
        using (var writer = new DelimitedStreamRowWriter(
                   text, new DelimitedRowWriterOptions { Columns = [.. columns] }))
        {
            writer.Write(row);
            writer.Flush();
        }

        return text.ToString();
    }

    private static JsonElement FirstRow(string json)
        => JsonDocument.Parse(json).RootElement[0];

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void ByteArrayIsBase64InJsonNotTheTypeName()
    {
        var json = WriteJson(new Dictionary<string, object?> { ["payload"] = new byte[] { 1, 2, 250 } });

        // The whole point: "System.Byte[]" is what Convert.ToString produced, and it is data loss
        // dressed up as a successful export.
        json.ShouldNotContain("System.Byte[]");
        FirstRow(json).GetProperty("payload").GetString().ShouldBe(Convert.ToBase64String([1, 2, 250]));
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void ByteArrayIsBase64InCsvNotTheTypeName()
    {
        var csv = WriteCsv(new Dictionary<string, object?> { ["payload"] = new byte[] { 1, 2, 250 } }, "payload");

        csv.ShouldNotContain("System.Byte[]");
        csv.ShouldContain(Convert.ToBase64String([1, 2, 250]));
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void DateTimeIsRoundTrippableIso8601InJson()
    {
        var json = WriteJson(new Dictionary<string, object?> { ["at"] = Timestamp });

        // Asserting the TEXT, not just that it parses: "09/22/2026 13:45:30" also parses back to
        // the same instant, so a parse-only assertion passed on the broken output and proved nothing.
        var written = FirstRow(json).GetProperty("at").GetString()!;
        written.ShouldStartWith("2026-09-22T13:45:30");
        written.ShouldNotContain("09/22/2026");
        DateTime.Parse(
            written,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind).ShouldBe(Timestamp);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void DateTimeIsRoundTrippableIso8601InCsv()
    {
        var csv = WriteCsv(new Dictionary<string, object?> { ["at"] = Timestamp }, "at").Trim();

        csv.ShouldContain("2026-09-22T13:45:30");
        csv.ShouldNotContain("09/22/2026");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void DateTimeOffsetKeepsItsOffset()
    {
        var value = new DateTimeOffset(2026, 9, 22, 13, 45, 30, TimeSpan.FromHours(-5));

        var json = WriteJson(new Dictionary<string, object?> { ["at"] = value });

        var written = FirstRow(json).GetProperty("at").GetString()!;
        written.ShouldStartWith("2026-09-22T13:45:30");
        written.ShouldEndWith("-05:00");
        DateTimeOffset.Parse(
            written,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind).ShouldBe(value);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void SpatialValueSerializesAsItsWellKnownTextNotItsRecordToString()
    {
        var point = new SpatialValue("POINT (-122.35 47.62)", 4326, SpatialValue.Geography);

        var json = WriteJson(new Dictionary<string, object?> { ["location"] = point });
        var csv = WriteCsv(new Dictionary<string, object?> { ["location"] = point }, "location");

        // A record's generated ToString ("SpatialValue { Wkt = ..., Srid = ... }") is a debugging
        // aid, never a serialization format.
        json.ShouldNotContain("SpatialValue {");
        csv.ShouldNotContain("SpatialValue {");
        var location = FirstRow(json).GetProperty("location").GetString();
        location.ShouldNotBeNull();
        location.ShouldContain("POINT (-122.35 47.62)");
        csv.ShouldContain("POINT (-122.35 47.62)");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void GuidIsCanonical()
    {
        var id = Guid.Parse("0199f8a1-0021-7000-a000-000000000001");

        FirstRow(WriteJson(new Dictionary<string, object?> { ["id"] = id }))
            .GetProperty("id").GetString().ShouldBe("0199f8a1-0021-7000-a000-000000000001");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void NumbersStayNumbersInJsonRatherThanBecomingStrings()
    {
        var json = FirstRow(WriteJson(new Dictionary<string, object?>
        {
            ["count"] = 42,
            ["ratio"] = 1.5d,
            ["ok"] = true,
        }));

        json.GetProperty("count").ValueKind.ShouldBe(JsonValueKind.Number);
        json.GetProperty("ratio").ValueKind.ShouldBe(JsonValueKind.Number);
        json.GetProperty("ok").ValueKind.ShouldBe(JsonValueKind.True);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void AnUnrecognisedTypeFailsLoudNamingTheTypeRatherThanStringifyingIt()
    {
        // Convert.ToString never fails, which is exactly how "System.Byte[]" reached an export.
        // A type with no defined serialized form has to say so.
        var failure = Should.Throw<NotSupportedException>(
            () => WriteJson(new Dictionary<string, object?> { ["thing"] = new UnknownShape() }));

        failure.Message.ShouldContain(nameof(UnknownShape));
        failure.Message.ShouldContain("thing");
    }

    private sealed class UnknownShape
    {
        public override string ToString() => "this must never reach the output";
    }
}
