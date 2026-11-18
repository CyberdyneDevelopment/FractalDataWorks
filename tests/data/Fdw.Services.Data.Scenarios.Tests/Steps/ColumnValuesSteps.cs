using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>Steps for GET .../fields/{FieldName}/values: the search of a column's distinct values.</summary>
[Binding]
internal sealed class ColumnValuesSteps
{
    internal const string ContainerVariable = "FDW_SCENARIO_VALUES_CONTAINER";

    private readonly ScenarioState _state;
    private string _column = string.Empty;

    public ColumnValuesSteps(ScenarioState state) => _state = state;

    [Given("a container whose column {string} holds these values")]
    public async Task ContainerHoldsValues(string column, DataTable expected)
    {
        _column = column;
        foreach (var row in expected.Rows)
        {
            var answer = await _state.SendAsAsync(Permission.Read, HttpMethod.Get, Url(column, $"q={Uri.EscapeDataString(row["value"])}&take=50"), null, CancellationToken.None).ConfigureAwait(false);
            answer.Status.ShouldBe(200, $"The fixture container named by {ContainerVariable} must be readable. {answer.Body}");
            using var json = answer.Json();
            var held = json.RootElement.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("value").GetString() == row["value"]);
            held.GetProperty("rows").GetInt32().ShouldBe(int.Parse(row["rows"], System.Globalization.CultureInfo.InvariantCulture), $"The fixture holds the wrong number of rows for {row["value"]}.");
        }
    }

    [When("the person searches the column for {string}")]
    public Task SearchesFor(string text) => Ask(_column, $"q={Uri.EscapeDataString(text)}");

    [When("the person searches the column {string} for {string}")]
    public Task SearchesColumnFor(string column, string text) => Ask(column, $"q={Uri.EscapeDataString(text)}");

    [When("the person asks for {int} values from the column starting at {int}")]
    public Task AsksForPage(int take, int skip) => Ask(_column, $"skip={skip}&take={take}");

    [When("the person asks for more values than the maximum a page may hold")]
    public Task AsksForTooMany() => Ask(_column, "take=1000000");

    [When("the person asks for the values of the column with {string} chosen")]
    public Task AsksWithChosen(string value) => Ask(_column, $"chosen={Uri.EscapeDataString(value)}");

    [When("the person asks for the values of the column with {string} chosen and left out")]
    public Task AsksWithChosenLeftOut(string value) => Ask(_column, $"chosen={Uri.EscapeDataString(value)}&leaveOutChosen=true");

    [When("the person asks for the values of the column with an empty value chosen")]
    public Task AsksWithEmptyChosen() => Ask(_column, "chosen=");

    [Then("the values answered are {string} and {string}")]
    public void ValuesAre(string first, string second) => Values().ShouldBe(new[] { first, second });

    [Then("every value carries the number of rows that hold it")]
    public void EveryValueHasRows()
    {
        using var json = _state.Answered.Json();
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        items.ShouldNotBeEmpty();
        items.All(item => item.GetProperty("rows").GetInt32() > 0).ShouldBeTrue();
    }

    [Then("no value is answered")]
    public void NoValue() => Values().ShouldBeEmpty();

    [Then("{int} value(s) is/are answered")]
    public void ValueCount(int count) => Values().Length.ShouldBe(count);

    [Then("the answer says more values match")]
    public void MoreMatch() => HasMore().ShouldBeTrue();

    [Then("the answer says no more values match")]
    public void NoMoreMatch() => HasMore().ShouldBeFalse();

    [Then("the value {string} is marked chosen")]
    public void MarkedChosen(string value) => Chosen(value).ShouldBeTrue();

    [Then("the value {string} is not marked chosen")]
    public void NotMarkedChosen(string value) => Chosen(value).ShouldBeFalse();

    [Then("the values answered do not include {string}")]
    public void ValuesDoNotInclude(string value) => Values().ShouldNotContain(value);

    private async Task Ask(string column, string query)
        => _state.Last = await _state.SendAsync(HttpMethod.Get, Url(column, query), null, CancellationToken.None).ConfigureAwait(false);

    private static string Url(string column, string query)
        => $"{ScenarioState.ContainerBase(ScenarioHost.Required(ContainerVariable))}/fields/{Uri.EscapeDataString(column)}/values?{query}";

    private string[] Values()
    {
        using var json = _state.Answered.Json();
        return json.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("value").GetString()!).ToArray();
    }

    private bool HasMore()
    {
        using var json = _state.Answered.Json();
        return json.RootElement.GetProperty("hasMore").GetBoolean();
    }

    private bool Chosen(string value)
    {
        using var json = _state.Answered.Json();
        return json.RootElement.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("value").GetString() == value).GetProperty("chosen").GetBoolean();
    }
}
