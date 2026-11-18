using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body of a data option path to check (FDW-6339).</summary>
public sealed class DataOptionPathCheckBody
{
    /// <summary>Gets or sets the path's names, from DataConfigurationRules down.</summary>
    public List<string>? Path { get; set; }

    /// <summary>Gets or sets the method the path ends in.</summary>
    public string? Method { get; set; }
}
