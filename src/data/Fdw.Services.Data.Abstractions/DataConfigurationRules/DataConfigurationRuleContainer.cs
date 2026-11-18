namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>The container a data configuration rule is about and is listed under (FDW-6345): the contract's route scope.</summary>
public sealed class DataConfigurationRuleContainer
{
    /// <summary>Gets the data store.</summary>
    public required string DataStore { get; init; }

    /// <summary>Gets the path of the data store.</summary>
    public required string DataPath { get; init; }

    /// <summary>Gets the container.</summary>
    public required string Container { get; init; }
}
