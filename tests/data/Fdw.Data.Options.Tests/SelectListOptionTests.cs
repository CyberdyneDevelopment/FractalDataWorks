using Fdw.Results;

namespace Fdw.Data.Options.Tests;

public sealed class SelectListOptionTests
{
    private static readonly IDataOption Include = DataOptions.ByName("SelectList").ByName("Include");

    private static readonly IDataOption Exclude = DataOptions.ByName("SelectList").ByName("Exclude");

    [Fact]
    public void IncludePutsTheValueInTheList()
    {
        Values(Include.Apply(Context("Include", "B", ["A"]))).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void IncludeDoesNotListAValueTwice()
    {
        Values(Include.Apply(Context("Include", "A", ["A", "B"]))).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void IncludeComparesValuesExactly()
    {
        Values(Include.Apply(Context("Include", "a", ["A"]))).ShouldBe(["A", "a"]);
    }

    [Fact]
    public void ExcludeTakesTheValueOutOfTheList()
    {
        Values(Exclude.Apply(Context("Exclude", "B", ["A", "B", "C"]))).ShouldBe(["A", "C"]);
    }

    [Fact]
    public void ExcludeOfAValueThatIsNotInTheListLeavesTheListAsItWas()
    {
        Values(Exclude.Apply(Context("Exclude", "Z", ["A", "B"]))).ShouldBe(["A", "B"]);
    }

    [Fact]
    public void AnInactiveIncludeLeavesTheListAsItWas()
    {
        Values(Include.Apply(Context("Include", "B", ["A"], isActive: false))).ShouldBe(["A"]);
    }

    [Fact]
    public void AnInactiveExcludeLeavesTheListAsItWas()
    {
        Values(Exclude.Apply(Context("Exclude", "A", ["A"], isActive: false))).ShouldBe(["A"]);
    }

    [Fact]
    public void AnInactiveRowWithNoValueIsNotAFailure()
    {
        Include.Apply(Context("Include", null, [], isActive: false)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ApplyDoesNotChangeTheListItWasGiven()
    {
        IReadOnlyList<string> given = ["A", "B"];

        Include.Apply(Context("Include", "C", given));
        Exclude.Apply(Context("Exclude", "A", given));

        given.ShouldBe(["A", "B"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ARowWithNoValueFailsWithTheValueRequiredCode(string? value)
    {
        var included = Include.Apply(Context("Include", value, []));
        var excluded = Exclude.Apply(Context("Exclude", value, ["A"]));

        included.IsFailure.ShouldBeTrue();
        included.Code!.Name.ShouldBe("DataOptionValueRequired");
        included.Code.Id.ShouldBe(20000);
        excluded.Code!.Name.ShouldBe("DataOptionValueRequired");
    }

    [Fact]
    public void TheOutcomeNamesTheOptionTypeAndTheOptionThatWereApplied()
    {
        var outcome = Include.Apply(Context("Include", "A", [])).Value.ShouldBeOfType<SelectListOutcome>();

        outcome.OptionType.ShouldBe("SelectList");
        outcome.Option.ShouldBe("Include");
    }

    [Fact]
    public void IncludeAppliesBeforeExclude()
    {
        Include.ApplyOrder.ShouldBeLessThan(Exclude.ApplyOrder);
    }

    [Fact]
    public void AValueThatIsIncludedAndExcludedEndsOutOfTheListWhenAppliedInOrder()
    {
        var included = Values(Include.Apply(Context("Include", "A", [])));

        Values(Exclude.Apply(Context("Exclude", "A", included))).ShouldBeEmpty();
    }

    private static IReadOnlyList<string> Values(IGenericResult<DataOptionOutcome> result) =>
        result.Value.ShouldBeOfType<SelectListOutcome>().Values;

    private static DataOptionContext Context(string option, string? value, IReadOnlyList<string> values, bool isActive = true) =>
        new("Store", "Container", "Column", "SelectList", option, value, isActive, values);
}
