using System.IO.Compression;
using System.Xml.Linq;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Delimited.Xlsx;

namespace Fdw.Data.RowSources.Tests;

/// <summary>FDW-1298: the Excel writer makes a workbook with the parts Excel needs, typed cells and a worksheet per limit of rows.</summary>
public sealed class XlsxRowWriterTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static ZipArchive Written(string[] columns, IEnumerable<Dictionary<string, object?>> rows, int rowsPerSheet = XlsxRowWriterOptions.MaxRowsPerSheet, string sheetName = "Data")
    {
        var body = new MemoryStream();
        var writer = new XlsxRowWriter(body, new XlsxRowWriterOptions { Columns = [.. columns], RowsPerSheet = rowsPerSheet, SheetName = sheetName });
        foreach (var row in rows)
        {
            writer.Write(row);
        }

        writer.Dispose();
        body.Position = 0;
        return new ZipArchive(body, ZipArchiveMode.Read);
    }

    private static Dictionary<string, object?> Row(params (string Column, object? Value)[] cells)
        => cells.ToDictionary(cell => cell.Column, cell => cell.Value);

    private static XDocument Part(ZipArchive archive, string path)
    {
        using var stream = archive.GetEntry(path)!.Open();
        return XDocument.Load(stream);
    }

    private static XElement? Cell(ZipArchive archive, int sheet, string reference)
        => Part(archive, $"xl/worksheets/sheet{sheet}.xml").Descendants(Main + "c").FirstOrDefault(c => (string?)c.Attribute("r") == reference);

    [Fact]
    public void TheWorkbookHasEveryPartExcelNeedsAndEachIsWellFormedXml()
    {
        using var archive = Written(["a"], [Row(("a", "x"))], sheetName: "People");

        foreach (var path in new[] { "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/styles.xml", "xl/worksheets/sheet1.xml" })
        {
            archive.GetEntry(path).ShouldNotBeNull(path);
            Part(archive, path).Root.ShouldNotBeNull(path);
        }

        Part(archive, "xl/workbook.xml").Descendants(Main + "sheet").Single().Attribute("name")!.Value.ShouldBe("People");
    }

    [Fact]
    public void TheHeaderRowIsBoldAndFrozenAndHoldsTheColumnNames()
    {
        using var archive = Written(["id", "name"], []);

        var header = Cell(archive, 1, "B1")!;
        header.Attribute("s")!.Value.ShouldBe("1");
        header.Descendants(Main + "t").Single().Value.ShouldBe("name");
        Part(archive, "xl/worksheets/sheet1.xml").Descendants(Main + "pane").Single().Attribute("state")!.Value.ShouldBe("frozen");
    }

    [Fact]
    public void NumbersAreNumbersTextIsInlineTextAndABooleanIsABoolean()
    {
        using var archive = Written(["n", "s", "b", "gap"], [Row(("n", 12.5m), ("s", "=1+1"), ("b", true), ("gap", null))]);

        Cell(archive, 1, "A2")!.Attribute("t").ShouldBeNull();
        Cell(archive, 1, "A2")!.Element(Main + "v")!.Value.ShouldBe("12.5");
        Cell(archive, 1, "B2")!.Attribute("t")!.Value.ShouldBe("inlineStr");
        Cell(archive, 1, "B2")!.Descendants(Main + "t").Single().Value.ShouldBe("=1+1");
        Cell(archive, 1, "C2")!.Attribute("t")!.Value.ShouldBe("b");
        Cell(archive, 1, "C2")!.Element(Main + "v")!.Value.ShouldBe("1");
        Cell(archive, 1, "D2").ShouldBeNull();
    }

    [Fact]
    public void ADateIsAnExcelSerialWithADateStyle()
    {
        using var archive = Written(["d", "day", "time"],
            [Row(("d", new DateTime(2020, 1, 1)), ("day", new DateOnly(2020, 1, 1)), ("time", new TimeOnly(12, 0)))]);

        Cell(archive, 1, "A2")!.Element(Main + "v")!.Value.ShouldBe("43831");
        Cell(archive, 1, "A2")!.Attribute("s")!.Value.ShouldBe("2");
        Cell(archive, 1, "B2")!.Attribute("s")!.Value.ShouldBe("3");
        Cell(archive, 1, "C2")!.Element(Main + "v")!.Value.ShouldBe("0.5");
        Cell(archive, 1, "C2")!.Attribute("s")!.Value.ShouldBe("4");
    }

    [Fact]
    public void WhatExcelCannotHoldIsWrittenAsTextNotRounded()
    {
        using var archive = Written(["big", "nan", "old"],
            [Row(("big", 1234567890123456789L), ("nan", double.NaN), ("old", new DateTime(1800, 1, 1)))]);

        Cell(archive, 1, "A2")!.Attribute("t")!.Value.ShouldBe("inlineStr");
        Cell(archive, 1, "A2")!.Descendants(Main + "t").Single().Value.ShouldBe("1234567890123456789");
        Cell(archive, 1, "B2")!.Attribute("t")!.Value.ShouldBe("inlineStr");
        Cell(archive, 1, "C2")!.Attribute("t")!.Value.ShouldBe("inlineStr");
        Cell(archive, 1, "C2")!.Descendants(Main + "t").Single().Value.ShouldStartWith("1800-01-01");
    }

    [Fact]
    public void RowsPastTheWorksheetLimitContinueOnAnotherWorksheetWithTheHeaderAgain()
    {
        var rows = Enumerable.Range(1, 5).Select(n => Row(("a", n)));

        using var archive = Written(["a"], rows, rowsPerSheet: 3, sheetName: "Data");

        Part(archive, "xl/workbook.xml").Descendants(Main + "sheet").Select(s => (string)s.Attribute("name")!).ToList()
            .ShouldBe(new[] { "Data", "Data (2)", "Data (3)" });
        Cell(archive, 2, "A1")!.Descendants(Main + "t").Single().Value.ShouldBe("a");
        Cell(archive, 2, "A2")!.Element(Main + "v")!.Value.ShouldBe("3");
        Cell(archive, 3, "A2")!.Element(Main + "v")!.Value.ShouldBe("5");
        Cell(archive, 3, "A3").ShouldBeNull();
    }

    [Fact]
    public void ADataSetWithNoRowsIsOneWorksheetWithTheHeaderRow()
    {
        using var archive = Written(["a", "b"], []);

        archive.Entries.Count(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)).ShouldBe(1);
        Cell(archive, 1, "B1").ShouldNotBeNull();
    }

    [Fact]
    public void ColumnsPastZAreNamedAA()
    {
        var columns = Enumerable.Range(0, 28).Select(n => "c" + n).ToArray();

        using var archive = Written(columns, [Row(("c26", "x"))]);

        Cell(archive, 1, "AA2")!.Descendants(Main + "t").Single().Value.ShouldBe("x");
        Cell(archive, 1, "AB1").ShouldNotBeNull();
    }

    [Fact]
    public void AWorksheetNameLosesWhatExcelForbidsAndStaysWithin31Characters()
    {
        using var archive = Written(["a"], [], sheetName: "a/b:c[d]" + new string('x', 40));

        var name = Part(archive, "xl/workbook.xml").Descendants(Main + "sheet").Single().Attribute("name")!.Value;
        name.Length.ShouldBe(31);
        name.ShouldStartWith("a_b_c_d_");
    }

    [Fact]
    public void TextExcelCannotHoldIsCutAndCleaned()
    {
        XlsxRowWriter.Clean("a\u0001b").ShouldBe("a\uFFFDb");
        XlsxRowWriter.Clean("\U0001F600").ShouldBe("\U0001F600");
        XlsxRowWriter.Clean("lone\uD800").ShouldBe("lone\uFFFD");
        XlsxRowWriter.Clean(new string('y', 40_000)).Length.ShouldBe(32_767);
    }

    [Fact]
    public void AWorkbookCannotBeBuiltWithoutColumnsOrWithAnImpossibleSheetSize()
    {
        Should.Throw<ArgumentException>(() => new XlsxRowWriter(new MemoryStream(), new XlsxRowWriterOptions()));
        Should.Throw<ArgumentOutOfRangeException>(() => new XlsxRowWriter(new MemoryStream(), new XlsxRowWriterOptions { Columns = ["a"], RowsPerSheet = 1 }));
    }

    [Fact]
    public void TheXlsxFormatIsBinaryAndRefusesATextWriter()
    {
        var format = RecordWriterTypes.ByName("Xlsx");

        format.WritesBinary.ShouldBeTrue();
        format.FileExtension.ShouldBe("xlsx");
        format.MediaType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        Should.Throw<NotSupportedException>(() => format.CreateWriter(new StringWriter(), format.CreateOptions(["a"])));
        RecordWriterTypes.ByAcceptHeader(format.MediaType).ShouldBeSameAs(format);
    }

    [Fact]
    public void AConnectionWritesABinaryFormatThroughTheGenericFormatDrivenPath()
    {
        var format = RecordWriterTypes.ByName("Xlsx");

        var bytes = RecordWriterBytes.Write(
            format, [], format.CreateOptions(["a"]),
            writer => ((IRowWriter)writer).Write(new Dictionary<string, object?> { ["a"] = "x" }));

        bytes.Take(2).ShouldBe(new[] { (byte)'P', (byte)'K' });
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        archive.GetEntry("xl/workbook.xml").ShouldNotBeNull();
    }

    [Fact]
    public void ATextFormatRefusesAByteContextAndABinaryFormatRefusesATextContext()
    {
        var delimited = RecordWriterTypes.ByName("Delimited");
        var xlsx = RecordWriterTypes.ByName("Xlsx");

        Should.Throw<NotSupportedException>(() => delimited.Create(RecordWriterContext.ForBytes(new MemoryStream(), [], delimited.CreateOptions(["a"]))));
        Should.Throw<NotSupportedException>(() => xlsx.Create(new RecordWriterContext(new StringWriter(), [], xlsx.CreateOptions(["a"]))));
        Should.Throw<NotSupportedException>(() => RecordWriterBytes.Write(delimited, [], delimited.CreateOptions(["a"]), _ => { }));
    }

    [Fact]
    public void ATextFormatIsNotBinaryAndRefusesAByteWriter()
    {
        var delimited = RecordWriterTypes.ByName("Delimited");

        delimited.WritesBinary.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => delimited.CreateBinaryWriter(new MemoryStream(), delimited.CreateOptions(["a"])));
    }
}
