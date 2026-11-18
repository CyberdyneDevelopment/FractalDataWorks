using System;

namespace Fdw.Services.Data.Scenarios.Tests.Steps;

/// <summary>
/// Where the scenarios run. Every value comes from the environment of the run and none has a default: a missing one stops the
/// scenario naming the variable, so a run never passes against a host nobody named.
/// </summary>
internal static class ScenarioHost
{
    internal const string AddressVariable = "FDW_SCENARIO_HOST";
    internal const string AddressWithoutTableVariable = "FDW_SCENARIO_HOST_WITHOUT_OPTIONS_TABLE";
    internal const string DataStoreVariable = "FDW_SCENARIO_DATASTORE";
    internal const string PathVariable = "FDW_SCENARIO_PATH";

    internal static string Required(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"The scenario run needs the environment variable {variable}, and it is not set. Nothing stands in for it.");
        return value;
    }

    internal static string TokenVariable(Permission permission) => permission switch
    {
        Permission.Read => "FDW_SCENARIO_TOKEN_READ",
        Permission.Write => "FDW_SCENARIO_TOKEN_WRITE",
        Permission.None => "FDW_SCENARIO_TOKEN_NONE",
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "The scenario names a permission this run does not know."),
    };
}
