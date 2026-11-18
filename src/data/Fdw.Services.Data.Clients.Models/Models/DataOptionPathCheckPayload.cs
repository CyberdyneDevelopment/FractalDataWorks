using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A data option path that resolves to a leaf (FDW-6339).</summary>
public sealed class DataOptionPathCheckPayload
{
    /// <summary>Gets or sets the path's names.</summary>
    public List<string> Path { get; set; } = [];

    /// <summary>Gets or sets the method the path ends in.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the family the path belongs to: the name of the level just below the DataConfigurationRules root.</summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>Gets or sets the fewest values the leaf takes.</summary>
    public int MinValues { get; set; }

    /// <summary>Gets or sets the most values the leaf takes; absent for a list of any length.</summary>
    public int? MaxValues { get; set; }
}
