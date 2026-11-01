using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>Tests for how the rows of a Files destination are split into the files they go to (FDW-3193).</summary>
public sealed class LoadFileSplitTests
{
    private static IDictionary<string, object?> Row(object? team, string name) =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Team"] = team, ["Name"] = name };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheFilesAreNamedByTheValueAndTheExtensionAndKeepTheOrderTheValuesFirstAppear()
    {
        var split = LoadFileSplit.Group("owner", ".csv", "team", [Row("Chiefs", "a"), Row("Broncos", "b"), Row("Chiefs", "c")], NullLogger.Instance);

        split.IsSuccess.ShouldBeTrue(split.CurrentMessage);
        split.Value!.Select(file => file.FileName).ShouldBe(["Chiefs.csv", "Broncos.csv"]);
        split.Value![0].Rows.Select(row => row["Name"]).ShouldBe(["a", "c"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NoRowsMakeNoFiles()
    {
        LoadFileSplit.Group("owner", ".csv", "Team", [], NullLogger.Instance).Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANullValueIsRefusedWithItsRowNumberAndNothingIsMadeUpForIt()
    {
        var split = LoadFileSplit.Group("owner", ".csv", "Team", [Row("Chiefs", "a"), Row(null, "b")], NullLogger.Instance);

        split.IsFailure.ShouldBeTrue();
        string.Concat(split.CurrentMessage).ShouldContain("row 2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueThatIsNotTextIsNamedByItsInvariantText()
    {
        var split = LoadFileSplit.Group("owner", ".csv", "Team", [Row(2026, "a"), Row(7.5m, "b")], NullLogger.Instance);

        split.Value!.Select(file => file.FileName).ShouldBe(["2026.csv", "7.5.csv"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueLongerThanAFileNameMayBeIsRefused()
    {
        var split = LoadFileSplit.Group("owner", ".csv", "Team", [Row(new string('x', 151), "a")], NullLogger.Instance);

        split.IsFailure.ShouldBeTrue();
        string.Concat(split.CurrentMessage).ShouldContain("150");
    }
}
