namespace Fdw.Services.Universes.Endpoints;

/// <summary>Addresses a universe by name.</summary>
public class UniverseNameRequest
{
    /// <summary>Gets or sets the universe name.</summary>
    public string Name { get; set; } = string.Empty;
}
