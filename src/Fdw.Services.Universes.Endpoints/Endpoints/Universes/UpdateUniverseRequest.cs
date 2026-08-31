namespace Fdw.Services.Universes.Endpoints;

/// <summary>Modifies a universe.</summary>
/// <remarks>
/// Every field except the name is nullable, and null means "leave it alone". PATCH modifies what
/// it is given; a null Status must not be read as a request to blank the status.
/// </remarks>
public class UpdateUniverseRequest
{
    /// <summary>Gets or sets the name of the universe to modify.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a new display name, or null to leave it.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets a new description, or null to leave it.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a new purpose, or null to leave it.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets a new status, or null to leave it.</summary>
    public string? Status { get; set; }

    /// <summary>Gets or sets a new visibility, or null to leave it.</summary>
    public string? Visibility { get; set; }

    /// <summary>Gets or sets a new join policy, or null to leave it.</summary>
    public string? JoinPolicy { get; set; }

    /// <summary>Gets or sets a new stand-in seed, or null to leave it.</summary>
    public string? StandInSeed { get; set; }
}
