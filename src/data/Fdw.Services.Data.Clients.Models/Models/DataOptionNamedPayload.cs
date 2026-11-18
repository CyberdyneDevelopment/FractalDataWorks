
namespace Fdw.Services.Data.Clients.Models;

/// <summary>A name a data option rule chooses from, with its display name (FDW-6339).</summary>
public sealed class DataOptionNamedPayload
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
