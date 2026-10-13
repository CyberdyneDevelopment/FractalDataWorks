using System.Linq;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1311: a container states whether its first row is a header, its field separator and its worksheet; an option
/// the container's format does not take is refused, and one that is not stated is never refused or defaulted.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public sealed class DataStoreContainerFileOptionsTests
{
    private static bool Refused(string? format, bool? hasHeader = null, string? separator = null, string? sheetName = null)
        => DataStoreContainerBuilder.FileOptionsRefusal("People", format, hasHeader, separator, sheetName, NullLogger.Instance) is not null;

    [Theory]
    [InlineData("Xlsx")]
    [InlineData("Delimited")]
    [InlineData("Csv")]
    [InlineData("Tsv")]
    [InlineData(null)]
    public void NothingStatedIsNeverRefused(string? format) => Refused(format).ShouldBeFalse();

    [Theory]
    [InlineData("Xlsx", false)]
    [InlineData("Delimited", false)]
    [InlineData("xlsx", false)]
    [InlineData("Csv", false)]
    [InlineData("Tsv", false)]
    [InlineData("Json", true)]
    [InlineData("Xml", true)]
    [InlineData(null, true)]
    public void HasHeaderIsOnlyForDelimitedAndXlsx(string? format, bool refused)
        => Refused(format, hasHeader: true).ShouldBe(refused);

    [Fact]
    public void AHeaderStatedAsNoIsAlsoAStatement()
    {
        Refused("Json", hasHeader: false).ShouldBeTrue();
        Refused("Delimited", hasHeader: false).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Delimited", ";", false)]
    [InlineData("Delimited", "\t", false)]
    [InlineData("Csv", ";", true)]
    [InlineData("Tsv", "\t", true)]
    [InlineData("Delimited", "", true)]
    [InlineData("Delimited", "12345678901", true)]
    [InlineData("Xlsx", ";", true)]
    [InlineData("Json", ";", true)]
    public void SeparatorIsOnlyForDelimitedAndOneToTenCharacters(string format, string separator, bool refused)
        => Refused(format, separator: separator).ShouldBe(refused);

    [Theory]
    [InlineData("Xlsx", "Staff", false)]
    [InlineData("Xlsx", "Q3 2026 (draft)", false)]
    [InlineData("Xlsx", "", true)]
    [InlineData("Xlsx", "  ", true)]
    [InlineData("Xlsx", "a/b", true)]
    [InlineData("Xlsx", "a:b", true)]
    [InlineData("Xlsx", "a[1]", true)]
    [InlineData("Xlsx", "what?", true)]
    [InlineData("Xlsx", "a*b", true)]
    [InlineData("Xlsx", "a\\b", true)]
    [InlineData("Delimited", "Staff", true)]
    [InlineData("Json", "Staff", true)]
    public void SheetNameIsOnlyForXlsxAndAWorksheetNameExcelHolds(string format, string sheetName, bool refused)
        => Refused(format, sheetName: sheetName).ShouldBe(refused);

    [Fact]
    public void AWorksheetNameIsAtMostThirtyOneCharacters()
    {
        Refused("Xlsx", sheetName: new string('a', 31)).ShouldBeFalse();
        Refused("Xlsx", sheetName: new string('a', 32)).ShouldBeTrue();
    }

    [Fact]
    public void TheNameRuleIsTheOneTheWriterCleansBy()
    {
        XlsxRowWriterOptions.MaxSheetNameLength.ShouldBe(31);
        "[]:*?/\\".All(XlsxRowWriterOptions.IsForbiddenInSheetName).ShouldBeTrue();
        XlsxRowWriterOptions.IsValidSheetName(null).ShouldBeFalse();
    }

    [Fact]
    public void ARefusalCarriesItsReasonForTheCaller()
    {
        var notForFormat = DataStoreContainerBuilder.FileOptionsRefusal("People", "Json", true, null, null, NullLogger.Instance)!;
        var sheet = DataStoreContainerBuilder.FileOptionsRefusal("People", "Xlsx", null, null, "a/b", NullLogger.Instance)!;
        var separator = DataStoreContainerBuilder.FileOptionsRefusal("People", "Delimited", null, "", null, NullLogger.Instance)!;

        notForFormat.Code!.Code.ShouldBe("DATA-21019");
        notForFormat.Code.FormatClientMessage(notForFormat.Details).ShouldBe("Container 'People' states HasHeader, which only a Delimited or Xlsx container takes");
        sheet.Code!.Code.ShouldBe("DATA-21020");
        sheet.Code.FormatClientMessage(sheet.Details).ShouldContain("worksheet 'a/b'");
        separator.Code!.Code.ShouldBe("DATA-21021");
        separator.Code.FormatClientMessage(separator.Details).ShouldContain("0 characters");
    }

    [Fact]
    public void NoReasonHoldsABackslashTheMessageGeneratorWouldNotEscape()
    {
        var sheet = DataStoreContainerBuilder.FileOptionsRefusal("People", "Xlsx", null, null, "a/b", NullLogger.Instance)!;

        sheet.Code!.FormatClientMessage(sheet.Details)!.ShouldNotContain("\\");
    }
}
