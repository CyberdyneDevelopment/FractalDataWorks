namespace Fdw.Services.Universes.Endpoints;

/// <summary>Creates a universe.</summary>
/// <remarks>
/// Status, Visibility and JoinPolicy are required rather than defaulted. A project silently
/// created Private and Closed, or silently Open, is a decision the caller did not make — so a
/// missing value is a validation failure, not a substituted one.
/// </remarks>
public class CreateUniverseRequest
{
    /// <summary>Gets or sets the unique universe name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status: Draft, Active, Paused or Archived.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project: Private, Discoverable or Open.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets the join policy: Closed, RequestToJoin or AutoApprove.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>Gets or sets the project-wide seed for generated stand-in values.</summary>
    public string? StandInSeed { get; set; }
}
