using System.Text;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>FDW-1298: a CSV a person opens in Excel can carry a byte order mark and cannot run a formula; the defaults leave both off.</summary>
public sealed class DelimitedExcelSafeOptionsTests
{
    private static string Written(DelimitedRowWriterOptions options, params Dictionary<string, object?>[] rows)
    {
        using var target = new StringWriter();
        using (var writer = new DelimitedStreamRowWriter(target, options))
        {
            foreach (var row in rows)
            {
                writer.Write(row);
            }
        }

        return target.ToString();
    }

    private static Dictionary<string, object?> Row(string column, object? value) => new() { [column] = value };

    [Fact]
    public void ByDefaultThereIsNoByteOrderMarkAndNothingIsGuarded()
    {
        var text = Written(new DelimitedRowWriterOptions { Columns = ["a"], WriteHeader = true }, Row("a", "=1+1"));

        text.ShouldNotStartWith("\uFEFF");
        text.ShouldContain("=1+1");
        text.ShouldNotContain("'=1+1");
    }

    [Fact]
    public void TheByteOrderMarkIsFirstEvenWithNoRows()
    {
        var text = Written(new DelimitedRowWriterOptions { Columns = ["a"], WriteHeader = true, WriteByteOrderMark = true });

        text.ShouldStartWith("\uFEFFa");
        Encoding.UTF8.GetBytes(text).Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-x", "'-x")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("-5", "'-5")]
    [InlineData("plain", "plain")]
    public void TextThatStartsLikeAFormulaGetsALeadingApostropheWhenAsked(string value, string written)
    {
        var text = Written(new DelimitedRowWriterOptions { Columns = ["a"], NeutralizeFormulas = true }, Row("a", value));

        text.TrimEnd('\r', '\n').ShouldBe(written);
    }

    [Fact]
    public void ANegativeNumberIsNotTextSoItIsLeftAlone()
    {
        var text = Written(new DelimitedRowWriterOptions { Columns = ["a"], NeutralizeFormulas = true }, Row("a", -5), Row("a", -2.5d));

        text.ShouldNotContain("'");
    }
}
