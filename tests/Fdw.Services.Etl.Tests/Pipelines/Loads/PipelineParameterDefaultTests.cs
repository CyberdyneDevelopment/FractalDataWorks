using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// FDW-1975: a pipeline parameter may declare a default, the designer's own text of the parameter's type. A run that does not give the
/// parameter uses it, a run that does keeps its own value, a parameter with no default stays absent so a setting that names it fails, and
/// the run's record says a defaulted parameter was not given.
/// </summary>
public sealed class PipelineParameterDefaultTests
{
    private static PipelineParameterConfiguration Declared(string name, string type, string? defaultText = null, bool required = false, bool shows = false) =>
        new() { Name = name, ParameterType = type, DefaultText = defaultText, IsRequired = required, ShowsValue = shows };

    private static List<KeyValuePair<string, string>> Supplied(params (string Name, string Value)[] pairs) =>
        pairs.Select(p => new KeyValuePair<string, string>(p.Name, p.Value)).ToList();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterTheRunDidNotGiveTakesItsDefaultReadAsItsType()
    {
        var result = PipelineRunParameterValues.Resolve([Declared("Limit", "Int64", "25"), Declared("File", "String", "orders.csv")], Supplied(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!["Limit"].ShouldBe(25L);
        result.Value["File"].ShouldBe("orders.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValueTheRunGaveBeatsTheDefault()
    {
        var result = PipelineRunParameterValues.Resolve([Declared("File", "String", "orders.csv")], Supplied(("file", "feed.csv")), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!["File"].ShouldBe("feed.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterWithNoDefaultStaysAbsent()
    {
        var result = PipelineRunParameterValues.Resolve([Declared("File", "String")], Supplied(), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ContainsKey("File").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ADefaultThatIsNotAValueOfTheTypeRefusesTheRunNamingTheParameter()
    {
        var result = PipelineRunParameterValues.Resolve([Declared("Limit", "Int64", "many")], Supplied(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Limit");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARequiredParameterIsStillRefusedWhenNotGiven()
    {
        var result = PipelineRunParameterValues.Resolve([Declared("File", "String", required: true)], Supplied(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("File");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheDefaultedParametersAreTheOnesWithADefaultThatTheRunDidNotGive()
    {
        IReadOnlyList<PipelineParameterConfiguration> declared = [Declared("A", "String", "a"), Declared("B", "String", "b"), Declared("C", "String")];

        var defaulted = PipelineRunParameterValues.Defaulted(declared, ["b"]);

        defaulted.ShouldBe(["A"], ignoreOrder: true);
        PipelineRunParameterValues.Defaulted(null, ["b"]).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheRunsRecordSaysADefaultedParameterWasNotGivenAndShowsItsValueOnlyWhenItsAuthorOptedIn()
    {
        IReadOnlyList<PipelineParameterConfiguration> declared = [Declared("File", "String", "orders.csv", shows: true), Declared("Other", "String", "x", shows: false), Declared("Mine", "String", shows: true)];
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["File"] = "orders.csv", ["Other"] = "x", ["Mine"] = "m" };

        var data = RunParameterEntries.EventData(declared, values, new HashSet<string>(["File", "Other"], StringComparer.OrdinalIgnoreCase));

        var entries = data.ShouldNotBeNull()["parameters"].ShouldBeAssignableTo<IEnumerable<IReadOnlyDictionary<string, object?>>>().ShouldNotBeNull().ToList();
        var file = entries.Single(entry => (string?)entry["name"] == "File");
        file["given"].ShouldBe(false);
        file["value"].ShouldBe("orders.csv");
        var other = entries.Single(entry => (string?)entry["name"] == "Other");
        other["given"].ShouldBe(false);
        other.ContainsKey("value").ShouldBeFalse();
        entries.Single(entry => (string?)entry["name"] == "Mine")["given"].ShouldBe(true);
    }
}
