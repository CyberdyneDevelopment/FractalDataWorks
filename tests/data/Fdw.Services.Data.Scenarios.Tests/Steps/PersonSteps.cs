using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>Steps that say who asks and which host answers.</summary>
[Binding]
internal sealed class PersonSteps
{
    private readonly ScenarioState _state;

    public PersonSteps(ScenarioState state) => _state = state;

    [Given("a person who holds datastores:read")]
    public void PersonHoldingRead() => _state.Person = Permission.Read;

    [Given("a person who holds datastores:write")]
    public void PersonHoldingWrite() => _state.Person = Permission.Write;

    [Given("a person who holds no permission")]
    public void PersonHoldingNothing() => _state.Person = Permission.None;

    [Given("the data options table is named by the server settings")]
    public void TableNamed() => _state.UseHostWithoutTable = false;

    [Given("the data options table is not named by the server settings")]
    public void TableNotNamed() => _state.UseHostWithoutTable = true;
}
