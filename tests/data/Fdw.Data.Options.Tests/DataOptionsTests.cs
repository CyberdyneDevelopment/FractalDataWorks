namespace Fdw.Data.Options.Tests;

public sealed class DataOptionsTests
{
    [Fact]
    public void SelectListIsAnOptionTypeThatResolvesByName()
    {
        var type = DataOptions.ByName("SelectList");

        ReferenceEquals(type, DataOptions.NotFound).ShouldBeFalse();
        type.Name.ShouldBe("SelectList");
        type.ShouldBeOfType<SelectListDataOptionType>();
    }

    [Fact]
    public void AStoredOptionTypeAndOptionResolveToTheOptionThroughTheDispatcher()
    {
        DataOptions.ByName("SelectList").ByName("Include").ShouldBeOfType<IncludeOption>();
        DataOptions.ByName("SelectList").ByName("Exclude").ShouldBeOfType<ExcludeOption>();
    }

    [Fact]
    public void TheOptionsOfSelectListAreIncludeAndExclude()
    {
        DataOptions.ByName("SelectList").Options.Select(option => option.Name).ShouldBe(["Exclude", "Include"], ignoreOrder: true);
    }

    [Fact]
    public void AnOptionTypeThatIsNotKnownGivesTheNotFoundSentinel()
    {
        ReferenceEquals(DataOptions.ByName("NoSuchType"), DataOptions.NotFound).ShouldBeTrue();
    }

    [Fact]
    public void AnOptionOfAnUnknownOptionTypeFailsWithTheOptionTypeNotFoundCodeAndDoesNotThrow()
    {
        var result = DataOptions.ByName("NoSuchType").ByName("Include").Apply(Context("NoSuchType", "Include"));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("DataOptionTypeNotFound");
        result.Code.Id.ShouldBe(31000);
    }

    [Fact]
    public void AnUnknownOptionOfSelectListGivesTheNotFoundSentinelOfItsCollection()
    {
        ReferenceEquals(SelectListOptions.ByName("NoSuchOption"), SelectListOptions.NotFound).ShouldBeTrue();
        ReferenceEquals(DataOptions.ByName("SelectList").ByName("NoSuchOption"), SelectListOptions.NotFound).ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownOptionOfSelectListFailsWithTheOptionNotFoundCodeAndDoesNotThrow()
    {
        var result = DataOptions.ByName("SelectList").ByName("NoSuchOption").Apply(Context("SelectList", "NoSuchOption"));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("DataOptionNotFound");
        result.Code.Id.ShouldBe(30000);
    }

    [Fact]
    public void AFailureCarriesTheNamesThatWereAskedFor()
    {
        var result = DataOptions.ByName("SelectList").ByName("NoSuchOption").Apply(Context("SelectList", "NoSuchOption"));

        result.Details!.Data["OptionType"].ShouldBe("SelectList");
        result.Details.Data["Option"].ShouldBe("NoSuchOption");
    }

    [Fact]
    public void AStoredRowAppliesFromTextAlone()
    {
        var result = DataOptions.ByName("SelectList").ByName("Include").Apply(Context("SelectList", "Include", "A"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<SelectListOutcome>().Values.ShouldBe(["A"]);
    }

    private static DataOptionContext Context(string optionType, string option, string? value = null) =>
        new("Store", "Container", "Column", optionType, option, value, true, []);
}
