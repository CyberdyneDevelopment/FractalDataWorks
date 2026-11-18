using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>Steps for GET /data-option-types.</summary>
[Binding]
internal sealed class OptionTypesSteps
{
    private readonly ScenarioState _state;

    public OptionTypesSteps(ScenarioState state) => _state = state;

    [When("the person asks for the data option types")]
    public async Task AsksForTypes()
        => _state.Last = await _state.SendAsync(HttpMethod.Get, "/data-option-types", null, CancellationToken.None).ConfigureAwait(false);

    [Then("the answer lists the option type {string} with the options {string} and {string} in that order")]
    public void ListsType(string typeName, string first, string second)
    {
        using var json = _state.Answered.Json();
        var type = json.RootElement.EnumerateArray().Single(entry => entry.GetProperty("name").GetString() == typeName);
        type.GetProperty("options").EnumerateArray().Select(option => option.GetString()).ToArray().ShouldBe(new[] { first, second });
    }
}
