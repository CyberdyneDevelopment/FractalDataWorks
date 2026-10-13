using System.IO.Compression;
using System.Text;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.Delimited.Xlsx;

namespace Fdw.Data.RowSources.Tests;

/// <summary>FDW-1308: an Excel workbook is read as rows, the counterpart of the Xlsx writer.</summary>
public sealed class XlsxRowSourceTests
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static MemoryStream Workbook(params (string Path, string Xml)[] parts)
    {
        var body = new MemoryStream();
        using (var zip = new ZipArchive(body, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, xml) in parts)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(xml);
            }
        }

        body.Position = 0;
        return body;
    }

    private static string WorkbookPart(bool date1904, params (string Name, string Id)[] sheets)
        => $"<workbook xmlns=\"{Main}\" xmlns:r=\"{Rel}\"><workbookPr date1904=\"{(date1904 ? 1 : 0)}\"/><sheets>"
            + string.Concat(sheets.Select((s, i) => $"<sheet name=\"{s.Name}\" sheetId=\"{i + 1}\" r:id=\"{s.Id}\"/>"))
            + "</sheets></workbook>";

    private static string Relationships(params (string Id, string Target)[] sheets)
        => "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            + string.Concat(sheets.Select(s => $"<Relationship Id=\"{s.Id}\" Type=\"{Rel}/worksheet\" Target=\"{s.Target}\"/>"))
            + "</Relationships>";

    private static string Sheet(string rows) => $"<worksheet xmlns=\"{Main}\"><sheetData>{rows}</sheetData></worksheet>";

    private static XlsxRowSourceOptions Options(int columns, bool header = false, string? sheetName = null)
        => new() { Columns = [.. Enumerable.Range(0, columns).Select(n => "c" + n)], HasHeader = header, SheetName = sheetName };

    private static List<object?[]> ReadAll(MemoryStream workbook, XlsxRowSourceOptions options)
    {
        using var source = new XlsxRowSource(workbook, options);
        var rows = new List<object?[]>();
        while (source.Read())
        {
            rows.Add([.. Enumerable.Range(0, source.FieldCount).Select(source.GetValue)]);
        }

        return rows;
    }

    // What Excel writes: shared strings with rich text runs and a phonetic run, styles with a built-in and a custom date
    // format and a time format, cells left out of a row, an error cell, a formula with its cached value and a blank row.
    private static MemoryStream ExcelShaped()
        => Workbook(
            ("xl/workbook.xml", WorkbookPart(false, ("People", "rId1"))),
            ("xl/_rels/workbook.xml.rels", Relationships(("rId1", "worksheets/sheet1.xml"))),
            ("xl/sharedStrings.xml", $"<sst xmlns=\"{Main}\"><si><t>Name</t></si>"
                + "<si><r><t>Ad</t></r><r><t xml:space=\"preserve\">a </t></r><r><t>Lovelace</t></r><rPh sb=\"0\" eb=\"1\"><t>ignored</t></rPh></si></sst>"),
            ("xl/styles.xml", $"<styleSheet xmlns=\"{Main}\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"[$-409]yyyy\\-mm\\-dd\\ hh:mm\"/></numFmts>"
                + "<cellXfs count=\"4\"><xf numFmtId=\"0\"/><xf numFmtId=\"14\"/><xf numFmtId=\"164\"/><xf numFmtId=\"21\"/></cellXfs></styleSheet>"),
            ("xl/worksheets/sheet1.xml", Sheet(
                "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c></row>"
                + "<row r=\"2\"><c r=\"A2\" t=\"s\"><v>1</v></c><c r=\"C2\"><v>12.5</v></c><c r=\"D2\" t=\"b\"><v>1</v></c>"
                + "<c r=\"E2\" s=\"1\"><v>43831</v></c><c r=\"F2\" t=\"e\"><v>#DIV/0!</v></c></row>"
                + "<row r=\"3\"/>"
                + "<row r=\"4\"><c r=\"A4\" t=\"inlineStr\"><is><t>inline</t></is></c><c r=\"B4\" s=\"2\"><v>43831.75</v></c>"
                + "<c r=\"C4\" s=\"3\"><v>0.5</v></c><c r=\"D4\"><f>1+1</f><v>2</v></c><c r=\"E4\"/></row>")));

    [Fact]
    public void WhatTheXlsxWriterWritesIsReadBackWithItsTypes()
    {
        var body = new MemoryStream();
        using (var writer = new XlsxRowWriter(body, new XlsxRowWriterOptions { Columns = ["id", "name", "n", "when", "flag", "time"] }))
        {
            writer.Write(new Dictionary<string, object?>
            {
                ["id"] = 7L, ["name"] = "Ada", ["n"] = 12.5m, ["when"] = new DateTime(2020, 1, 1, 6, 30, 0), ["flag"] = true, ["time"] = new TimeOnly(12, 0),
            });
        }

        body.Position = 0;
        var rows = ReadAll(body, Options(6, header: true));

        rows.Count.ShouldBe(1);
        rows[0].ShouldBe([7L, "Ada", 12.5m, new DateTime(2020, 1, 1, 6, 30, 0), true, new TimeOnly(12, 0)]);
    }

    [Fact]
    public void AWorkbookAsExcelWritesItIsReadByColumnLetterWithItsStringsAndDates()
    {
        var rows = ReadAll(ExcelShaped(), Options(6, header: true));

        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(["Ada Lovelace", null, 12.5m, true, new DateTime(2020, 1, 1), "#DIV/0!"]);
        rows[1].ShouldBe(["inline", new DateTime(2020, 1, 1, 18, 0, 0), new TimeOnly(12, 0), 2L, null, null]);
    }

    [Fact]
    public void ABlankRowIsSkippedAndTheHeaderIsTheFirstNonEmptyRow()
    {
        using var source = new XlsxRowSource(ExcelShaped(), Options(3, header: true));

        source.Read().ShouldBeTrue();
        source.GetValue(0).ShouldBe("Ada Lovelace");
        source.Read().ShouldBeTrue();
        source.GetValue(0).ShouldBe("inline");
        source.Read().ShouldBeFalse();
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    public void MaxRowsStopsTheReadAndAnUnnamedWorksheetOfSeveralIsRefused()
    {
        var two = () => Workbook(
            ("xl/workbook.xml", WorkbookPart(false, ("First", "rId1"), ("Second", "rId2"))),
            ("xl/_rels/workbook.xml.rels", Relationships(("rId1", "worksheets/sheet1.xml"), ("rId2", "worksheets/sheet2.xml"))),
            ("xl/worksheets/sheet1.xml", Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row><row r=\"2\"><c r=\"A2\"><v>2</v></c></row>")),
            ("xl/worksheets/sheet2.xml", Sheet("<row r=\"1\"><c r=\"A1\"><v>9</v></c></row>")));

        Should.Throw<InvalidDataException>(() => ReadAll(two(), Options(1))).Message.ShouldContain("'First', 'Second'");
        var second = ReadAll(two(), Options(1, sheetName: "second"));
        second.Count.ShouldBe(1);
        second[0].ShouldBe([9L]);
        Should.Throw<InvalidDataException>(() => ReadAll(two(), Options(1, sheetName: "Third"))).Message.ShouldContain("'Third'");
        var capped = Options(1, sheetName: "First");
        capped.MaxRows = 1;
        var first = ReadAll(two(), capped);
        first.Count.ShouldBe(1);
        first[0].ShouldBe([1L]);
    }

    [Fact]
    public void ADateCountsFromTheCalendarTheWorkbookDeclares()
    {
        object?[] Read(bool date1904, string serial) => ReadAll(
            Workbook(
                ("xl/workbook.xml", WorkbookPart(date1904, ("S", "rId1"))),
                ("xl/_rels/workbook.xml.rels", Relationships(("rId1", "worksheets/sheet1.xml"))),
                ("xl/styles.xml", $"<styleSheet xmlns=\"{Main}\"><cellXfs count=\"1\"><xf numFmtId=\"14\"/></cellXfs></styleSheet>"),
                ("xl/worksheets/sheet1.xml", Sheet($"<row r=\"1\"><c r=\"A1\" s=\"0\"><v>{serial}</v></c></row>"))),
            Options(1))[0];

        Read(false, "43831")[0].ShouldBe(new DateTime(2020, 1, 1));
        Read(true, "42369")[0].ShouldBe(new DateTime(2020, 1, 1));
        Read(false, "59")[0].ShouldBe(new DateTime(1900, 2, 28));
        Read(false, "61")[0].ShouldBe(new DateTime(1900, 3, 1));
        Should.Throw<InvalidDataException>(() => Read(false, "60"));
    }

    [Fact]
    public void ContentThatIsNotAWorkbookIsRefusedNamingWhatIsWrong()
    {
        using var source = new XlsxRowSource(new MemoryStream("a,b\r\n1,2"u8.ToArray()), Options(2));

        Should.Throw<InvalidDataException>(() => source.Read());
    }

    [Fact]
    public void TheReaderNeedsColumnsAndTheTypeIsBinaryAndRegistered()
    {
        Should.Throw<ArgumentException>(() => new XlsxRowSource(new MemoryStream(), new XlsxRowSourceOptions()));

        var type = RecordSourceTypes.ByName("Xlsx");
        type.ReadsBinary.ShouldBeTrue();
        type.Format.ShouldBe("Xlsx");
        RecordSourceTypes.ByName("Delimited").ReadsBinary.ShouldBeFalse();
        Should.Throw<ArgumentException>(() => type.CreateReader(new MemoryStream(), null));
        using var reader = type.CreateReader(ExcelShaped(), Options(3, header: true));
        reader.Read().ShouldBeTrue();
    }
}
