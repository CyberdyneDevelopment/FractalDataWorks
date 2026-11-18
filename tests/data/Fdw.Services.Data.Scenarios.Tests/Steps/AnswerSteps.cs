using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>Steps that check the last answer: its status and the codes it carries.</summary>
[Binding]
internal sealed class AnswerSteps
{
    private readonly ScenarioState _state;

    public AnswerSteps(ScenarioState state) => _state = state;

    [Then("the answer status is {int}")]
    public void StatusIs(int status) => Last().Status.ShouldBe(status, Last().Body);

    [Then("the answer carries the code {word}")]
    public void CarriesCode(string code) => Last().Body.ShouldContain(code, Case.Sensitive, Last().Body);

    [Then("the answer carries the codes {word} and {word}")]
    public void CarriesCodes(string first, string second)
    {
        Last().Body.ShouldContain(first, Case.Sensitive, Last().Body);
        Last().Body.ShouldContain(second, Case.Sensitive, Last().Body);
    }

    [Then("the answer names row {int} with DATA-21676")]
    public void NamesRow(int position)
    {
        Last().Body.ShouldContain("DATA-21676", Case.Sensitive, Last().Body);
        Last().Body.ShouldContain($"Row {position} of the add is refused", Case.Sensitive, Last().Body);
    }

    private Answer Last() => _state.Answered;
}
