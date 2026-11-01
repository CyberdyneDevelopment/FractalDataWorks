using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.RoundTrip.Tests;

/// <summary>
/// Proves the JSON writer emits each row as it is written rather than holding the whole document
/// until <c>Flush</c>. Held until the end, memory scales with the row count, which makes the writer
/// unusable for a large export and unbounded for an ETL file sink.
/// </summary>
public class JsonStreamingWriteTests
{
    // Why a TextWriter that records each Write call: the question is not what the target ended up
    // holding but whether anything reached it before the document was finished.
    private sealed class RecordingTextWriter : TextWriter
    {
        private readonly StringBuilder _all = new();

        public List<int> WriteLengths { get; } = [];

        public override Encoding Encoding => Encoding.UTF8;

        public string Text => _all.ToString();

        public override void Write(char[] buffer, int index, int count)
        {
            if (count > 0) WriteLengths.Add(count);
            _all.Append(buffer, index, count);
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            WriteLengths.Add(value!.Length);
            _all.Append(value);
        }

        public override void Write(char value)
        {
            WriteLengths.Add(1);
            _all.Append(value);
        }
    }

    private static Dictionary<string, object?> Row(long id) =>
        new() { ["id"] = id, ["name"] = "row-" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void Rows_reach_the_target_before_the_document_is_finished()
    {
        var target = new RecordingTextWriter();
        using var writer = new JsonStreamRowWriter(target);

        writer.Write(Row(1));
        writer.Write(Row(2));

        // No Flush yet: with the old buffer-everything behaviour the target would still be empty.
        target.Text.ShouldNotBeEmpty();
        target.Text.ShouldContain("row-1");
        target.Text.ShouldContain("row-2");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void A_large_write_reaches_the_target_in_many_pieces_not_one()
    {
        var target = new RecordingTextWriter();
        using (var writer = new JsonStreamRowWriter(target))
        {
            for (var i = 0; i < 500; i++)
            {
                writer.Write(Row(i));
            }

            writer.Flush();
        }

        // One write would mean the whole document was assembled in memory first.
        target.WriteLengths.Count.ShouldBeGreaterThan(400);
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void Draining_each_row_still_produces_one_valid_json_array()
    {
        var target = new RecordingTextWriter();
        using (var writer = new JsonStreamRowWriter(target))
        {
            for (var i = 0; i < 25; i++)
            {
                writer.Write(Row(i));
            }

            writer.Flush();
        }

        using var parsed = System.Text.Json.JsonDocument.Parse(target.Text);
        parsed.RootElement.ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);
        parsed.RootElement.GetArrayLength().ShouldBe(25);
        parsed.RootElement[24].GetProperty("name").GetString().ShouldBe("row-24");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void A_multi_byte_character_survives_the_per_row_drain()
    {
        var target = new RecordingTextWriter();
        using (var writer = new JsonStreamRowWriter(target))
        {
            writer.Write(new Dictionary<string, object?> { ["name"] = "\U0001F600 café 日本語" });
            writer.Flush();
        }

        using var parsed = System.Text.Json.JsonDocument.Parse(target.Text);
        parsed.RootElement[0].GetProperty("name").GetString().ShouldBe("\U0001F600 café 日本語");
    }

    [Fact]
    [Trait("Category", "RoundTrip")]
    public void An_empty_write_still_produces_an_empty_array()
    {
        var target = new RecordingTextWriter();
        using (var writer = new JsonStreamRowWriter(target))
        {
            writer.Flush();
        }

        using var parsed = System.Text.Json.JsonDocument.Parse(target.Text);
        parsed.RootElement.GetArrayLength().ShouldBe(0);
    }
}
