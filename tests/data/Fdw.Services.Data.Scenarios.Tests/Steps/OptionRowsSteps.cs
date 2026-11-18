using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>Steps for the option row routes: list, add, change and remove rows of a container.</summary>
[Binding]
internal sealed class OptionRowsSteps
{
    private const string Division = "Division";
    private const string SelectList = "SelectList";
    private const string Include = "Include";

    private readonly ScenarioState _state;

    public OptionRowsSteps(ScenarioState state) => _state = state;

    [Given("the container {string} has no option rows")]
    public async Task ContainerHasNoRows(string container)
    {
        _state.Wrote(container);
        await OptionRowCalls.RemoveAllAsync(_state, container, CancellationToken.None).ConfigureAwait(false);
    }

    [Given("the container {string} has the columns {string} and {string}")]
    public async Task ContainerHasColumns(string container, string first, string second)
    {
        foreach (var column in new[] { first, second })
        {
            var answer = await _state.SendAsAsync(Permission.Read, HttpMethod.Get, $"{ScenarioState.ContainerBase(container)}/fields/{Uri.EscapeDataString(column)}/values?take=1", null, CancellationToken.None).ConfigureAwait(false);
            answer.Status.ShouldBe(200, $"The container {container} must have the column {column}. {answer.Body}");
        }
    }

    [Given("the container {string} has these option rows")]
    public async Task ContainerHasRows(string container, DataTable rows)
    {
        _state.Wrote(container);
        var answer = await _state.SendAsAsync(Permission.Write, HttpMethod.Post, OptionRowCalls.Base(container), OptionRowCalls.AddBody(Parse(rows)), CancellationToken.None).ConfigureAwait(false);
        answer.Status.ShouldBe(201, answer.Body);
    }

    [When("a person who holds datastores:read lists the rows of the container {string}")]
    public async Task ReaderLists(string container)
    {
        _state.Person = Permission.Read;
        _state.Last = await _state.SendAsync(HttpMethod.Get, OptionRowCalls.Base(container), null, CancellationToken.None).ConfigureAwait(false);
    }

    [When("a person who holds datastores:read lists the rows of the container {string} for the column {string}")]
    public async Task ReaderListsColumn(string container, string column)
    {
        _state.Person = Permission.Read;
        _state.Last = await _state.SendAsync(HttpMethod.Get, $"{OptionRowCalls.Base(container)}?column={Uri.EscapeDataString(column)}", null, CancellationToken.None).ConfigureAwait(false);
    }

    [When("the person lists the rows of the container {string}")]
    public Task PersonLists(string container)
        => Send(HttpMethod.Get, OptionRowCalls.Base(container), null);

    [When("the person adds these rows to the container {string}")]
    public Task AddsRows(string container, DataTable rows)
        => Add(container, OptionRowCalls.AddBody(Parse(rows)));

    [When("the person adds no rows to the container {string}")]
    public Task AddsNoRows(string container) => Add(container, OptionRowCalls.AddBody([]));

    [When("the person adds a row with the value {string} to the container {string}")]
    public Task AddsRowWithValue(string value, string container) => Add(container, OptionRowCalls.AddBody([(Division, value, SelectList, Include, true)]));

    [When("the person adds the column {string} and the value {string} twice to the container {string}")]
    public Task AddsPairTwice(string column, string value, string container)
        => Add(container, OptionRowCalls.AddBody([(column, value, SelectList, Include, true), (column, value, SelectList, Include, true)]));

    [When("the person adds a row that does not state active to the container {string}")]
    public Task AddsRowWithoutActive(string container) => Add(container, OptionRowCalls.AddBody([(Division, "East", SelectList, Include, null)]));

    [When("the person adds the column {string} and the value {string} to the container {string}")]
    public Task AddsPair(string column, string value, string container) => Add(container, OptionRowCalls.AddBody([(column, value, SelectList, Include, true)]));

    [When("the person adds a row with the option type {string} to the container {string}")]
    public Task AddsRowWithType(string optionType, string container) => Add(container, OptionRowCalls.AddBody([(Division, "East", optionType, Include, true)]));

    [When("the person adds a row with the option {string} to the container {string}")]
    public Task AddsRowWithOption(string option, string container) => Add(container, OptionRowCalls.AddBody([(Division, "East", SelectList, option, true)]));

    [When("the person adds a row with the column {string} to the container {string}")]
    public Task AddsRowWithColumn(string column, string container) => Add(container, OptionRowCalls.AddBody([(column, "East", SelectList, Include, true)]));

    [When("the person adds one good row and one row with the option type {string} to the container {string}")]
    public Task AddsGoodAndBad(string optionType, string container)
        => Add(container, OptionRowCalls.AddBody([(Division, "East", SelectList, Include, true), (Division, "West", optionType, Include, true)]));

    [When("the person adds a row to the container {string}")]
    public Task AddsOneRow(string container) => Add(container, OptionRowCalls.AddBody([(Division, "East", SelectList, Include, true)]));

    [When("the person changes the option of the row {string} to {string} and switches it off")]
    public Task ChangesOption(string value, string option) => Change(value, null, JsonSerializer.Serialize(new { option, active = false }));

    [When("the person changes the row {string} and states no change")]
    public Task ChangesNothing(string value) => Change(value, null, "{}");

    [When("the person changes the value of the row {string} to {string}")]
    public Task ChangesValue(string value, string newValue) => Change(value, null, JsonSerializer.Serialize(new { value = newValue }));

    [When("the person changes the row {string} through the container {string}")]
    public Task ChangesThrough(string value, string container) => Change(value, container, JsonSerializer.Serialize(new { active = false }));

    [When("the person removes the rows {string} and {string}")]
    public Task RemovesRows(string first, string second) => Remove([first, second], null);

    [When("the person removes the rows {string} and {string} through the container {string}")]
    public Task RemovesRowsThrough(string first, string second, string container) => Remove([first, second], container);

    [When("the person removes no rows from the container {string}")]
    public Task RemovesNone(string container) => Send(HttpMethod.Delete, OptionRowCalls.Base(container), null);

    [Then("the rows answered are in this order")]
    public void RowsInOrder(DataTable expected)
    {
        using var json = _state.Answered.Json();
        json.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => (item.GetProperty("column").GetString(), item.GetProperty("value").GetString()))
            .ToArray()
            .ShouldBe(expected.Rows.Select(row => ((string?)row["column"], (string?)row["value"])).ToArray());
    }

    [Then("every row carries its id")]
    public void EveryRowHasId()
    {
        using var json = _state.Answered.Json();
        json.RootElement.GetProperty("items").EnumerateArray().All(item => Guid.TryParse(item.GetProperty("id").GetString(), out _)).ShouldBeTrue();
    }

    [Then("{int} row(s) is/are answered, each with an id")]
    public void RowsAnswered(int count)
    {
        using var json = _state.Answered.Json();
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToArray();
        items.Length.ShouldBe(count);
        items.All(item => Guid.TryParse(item.GetProperty("id").GetString(), out _)).ShouldBeTrue();
    }

    [Then("the container {string} holds {int} option row(s)")]
    public async Task ContainerHolds(string container, int count)
        => (await OptionRowCalls.ListAsync(_state, Permission.Write, container, CancellationToken.None).ConfigureAwait(false)).Count.ShouldBe(count);

    [Then("the row {string} answers the option {string} and active false")]
    public void RowAnswers(string value, string option)
    {
        using var json = _state.Answered.Json();
        json.RootElement.GetProperty("option").GetString().ShouldBe(option);
        json.RootElement.GetProperty("active").GetBoolean().ShouldBeFalse();
        json.RootElement.GetProperty("value").GetString().ShouldBe(value);
    }

    private static List<(string Column, string Value, string OptionType, string Option, bool? Active)> Parse(DataTable rows)
        => rows.Rows.Select(row => (row["column"], row["value"], row["optionType"], row["option"], (bool?)bool.Parse(row["active"]))).ToList();

    private async Task Send(HttpMethod method, string relativeUrl, string? json)
        => _state.Last = await _state.SendAsync(method, relativeUrl, json, CancellationToken.None).ConfigureAwait(false);

    private Task Add(string container, string body)
    {
        _state.Wrote(container);
        return Send(HttpMethod.Post, OptionRowCalls.Base(container), body);
    }

    private async Task Change(string value, string? throughContainer, string body)
    {
        var (container, id) = await OptionRowCalls.FindAsync(_state, value, CancellationToken.None).ConfigureAwait(false);
        await Send(new HttpMethod("PATCH"), $"{OptionRowCalls.Base(throughContainer ?? container)}/{id}", body).ConfigureAwait(false);
    }

    private async Task Remove(string[] values, string? throughContainer)
    {
        var found = new List<(string Container, string Id)>();
        foreach (var value in values)
            found.Add(await OptionRowCalls.FindAsync(_state, value, CancellationToken.None).ConfigureAwait(false));
        var query = string.Join('&', found.Select(row => $"id={Uri.EscapeDataString(row.Id)}"));
        await Send(HttpMethod.Delete, $"{OptionRowCalls.Base(throughContainer ?? found[0].Container)}?{query}", null).ConfigureAwait(false);
    }
}
