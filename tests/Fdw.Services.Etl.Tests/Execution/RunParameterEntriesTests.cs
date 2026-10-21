using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// A run's record says which declared parameters it was given, and shows a value only for a parameter whose author opted in (FDW-1756).
/// </summary>
public sealed class RunParameterEntriesTests
{
    private static PipelineParameterConfiguration Parameter(string name, string type, bool shows, bool isList = false) =>
        new() { Name = name, ParameterType = type, ShowsValue = shows, IsList = isList };

    private static List<IReadOnlyDictionary<string, object?>> Entries(IReadOnlyDictionary<string, object?>? data) =>
        [.. data.ShouldNotBeNull()["parameters"].ShouldBeAssignableTo<IEnumerable<IReadOnlyDictionary<string, object?>>>().ShouldNotBeNull()];

    private static IReadOnlyDictionary<string, object?> Values(params (string Name, object Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Name, pair => (object?)pair.Value, System.StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AnOptedInParameterRecordsItsValue()
    {
        var data = RunParameterEntries.EventData([Parameter("Which", "Text", shows: true)], Values(("Which", "BreweryFeed")));

        var entry = Entries(data).ShouldHaveSingleItem();
        entry["name"].ShouldBe("Which");
        entry["given"].ShouldBe(true);
        entry["value"].ShouldBe("BreweryFeed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AParameterThatDidNotOptInIsRecordedAsGivenAndItsValueAppearsNowhere()
    {
        const string secret = "s3cr3t-value-123";
        var data = RunParameterEntries.EventData([Parameter("Token", "Text", shows: false)], Values(("Token", secret)));

        var entry = Entries(data).ShouldHaveSingleItem();
        entry["name"].ShouldBe("Token");
        entry["given"].ShouldBe(true);
        entry.ContainsKey("value").ShouldBeFalse();
        JsonSerializer.Serialize(data).ShouldNotContain(secret);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void OnlyTheOptedInParameterShowsItsValueWhenBothAreGiven()
    {
        var data = RunParameterEntries.EventData(
            [Parameter("Which", "Text", shows: true), Parameter("Token", "Text", shows: false)],
            Values(("Which", "BreweryFeed"), ("Token", "hidden")));

        var entries = Entries(data);
        entries.Count.ShouldBe(2);
        entries[0]["value"].ShouldBe("BreweryFeed");
        entries[1].ContainsKey("value").ShouldBeFalse();
        JsonSerializer.Serialize(data).ShouldNotContain("hidden");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterTheRunWasNotGivenHasNoEntry()
    {
        var data = RunParameterEntries.EventData(
            [Parameter("Which", "Text", shows: true), Parameter("Size", "Int32", shows: true)], Values(("Size", 50)));

        var entry = Entries(data).ShouldHaveSingleItem();
        entry["name"].ShouldBe("Size");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ARunGivenNoDeclaredParameterHasNoEvent()
    {
        RunParameterEntries.EventData([Parameter("Which", "Text", shows: true)], Values()).ShouldBeNull();
        RunParameterEntries.EventData(null, Values(("Which", "x"))).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValueIsWrittenAsTextTheWayItArrived()
    {
        var moment = new System.DateTimeOffset(2026, 10, 19, 1, 2, 3, System.TimeSpan.Zero);
        var data = RunParameterEntries.EventData(
            [
                Parameter("Flag", "Boolean", shows: true),
                Parameter("Size", "Int32", shows: true),
                Parameter("Since", "DateTimeOffset", shows: true),
                Parameter("Names", "Text", shows: true, isList: true),
            ],
            Values(("Flag", true), ("Size", 50), ("Since", moment), ("Names", new List<object> { "a", "b" })));

        var entries = Entries(data);
        entries[0]["value"].ShouldBe("true");
        entries[1]["value"].ShouldBe("50");
        entries[2]["value"].ShouldBe("2026-10-19T01:02:03.0000000+00:00");
        entries[3]["value"].ShouldBe("[\"a\",\"b\"]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParameterIsFoundWhateverTheCaseOfItsName()
    {
        var data = RunParameterEntries.EventData([Parameter("Which", "Text", shows: true)], Values(("WHICH", "x")));

        Entries(data).ShouldHaveSingleItem()["value"].ShouldBe("x");
    }
}
