using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1324: a store that finds a container as a file adds the format's extension itself, so a container named with that
/// extension could never be found; it is refused at create, and only for such a store.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public sealed class DataStoreContainerNameRefusalTests
{
    private static bool Refused(string name, string? format, bool fileStore)
        => DataStoreContainerBuilder.NameRefusal(name, format, fileStore, NullLogger.Instance) is not null;

    [Theory]
    [InlineData("earthquakes-export.xlsx", "Xlsx", true)]
    [InlineData("Earthquakes-Export.XLSX", "Xlsx", true)]
    [InlineData("people.csv", "Delimited", true)]
    [InlineData("feed.json", "Json", true)]
    [InlineData("feed.xml", "Xml", true)]
    [InlineData("export.txt", "FixedWidth", true)]
    public void AFileContainerNamedWithItsFormatsExtensionIsRefused(string name, string format, bool fileStore)
        => Refused(name, format, fileStore).ShouldBeTrue();

    [Theory]
    [InlineData("earthquakes-export", "Xlsx", true)]
    [InlineData("people.xlsx", "Delimited", true)]
    [InlineData("data.xlsx", "Xlsx", false)]
    [InlineData("rows", "Tabular", true)]
    [InlineData("data.json", "Tabular", true)]
    [InlineData("data.xlsx", null, true)]
    [InlineData("data.xlsx", "", true)]
    [InlineData("data.notfound", "NoSuchFormat", true)]
    [InlineData("data.xlsx", "NoSuchFormat", true)]
    public void AnythingElseIsLeftAlone(string name, string? format, bool fileStore)
        => Refused(name, format, fileStore).ShouldBeFalse();

    [Fact]
    public void TheRefusalSaysWhatToNameTheContainer()
    {
        var refusal = DataStoreContainerBuilder.NameRefusal("earthquakes-export.xlsx", "Xlsx", true, NullLogger.Instance)!;

        refusal.Code!.Code.ShouldBe("DATA-21022");
        refusal.Code.FormatClientMessage(refusal.Details)
            .ShouldBe("Container 'earthquakes-export.xlsx' is named with its format's own extension '.xlsx'; the store adds the extension itself, so name the container 'earthquakes-export'");
    }
}
