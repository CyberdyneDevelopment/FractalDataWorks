using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reqnroll;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>
/// Before each scenario, skips it when no host is named, so a whole-solution run without a host reports the scenarios as skipped (no evidence)
/// and is not red. A skipped scenario is never a pass. When a host is named, every other variable is required and a missing one fails the scenario.
/// After each scenario, removes the option rows it wrote, so a scenario leaves the host as it found it and runs alone or in any order.
/// </summary>
[Binding]
internal sealed class ScenarioHooks
{
    private readonly ScenarioState _state;

    public ScenarioHooks(ScenarioState state) => _state = state;

    [BeforeScenario(Order = 0)]
    public static void SkipWhenNoHostIsNamed()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ScenarioHost.AddressVariable)))
            Assert.Skip($"No host is named: set {ScenarioHost.AddressVariable} to run the data option route scenarios.");
    }

    [AfterScenario]
    public async Task RemoveWhatWasWritten()
    {
        foreach (var container in _state.WrittenContainers.ToArray())
            await OptionRowCalls.RemoveAllAsync(_state, container, CancellationToken.None).ConfigureAwait(false);
    }
}
