namespace Fdw.Services.Etl.Projects.Clients;

/// <summary>
/// Request body for the unified trigger endpoint (POST /etl/trigger/{type}).
/// Either <see cref="Id"/> OR (<see cref="Name"/> + <see cref="ParentPath"/>) must be supplied.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TriggerRequest
{
    /// <summary>
    /// Gets or sets the logical identifier of the item to trigger.
    /// Takes precedence over Name when both are provided.
    /// </summary>
    public System.Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the item to trigger.
    /// Required when <see cref="Id"/> is null for Stage and Step types.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parent path for name-based resolution (e.g., "ProjectName/StageName").
    /// Required for Stage and Step trigger types when using name-based lookup.
    /// </summary>
    public string? ParentPath { get; set; }

    /// <summary>
    /// Gets or sets the source that initiated this trigger request (e.g., "UI", "Schedule:Daily", "API").
    /// Used for audit and lineage tracking.
    /// </summary>
    public string? TriggerSource { get; set; }
}
