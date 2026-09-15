using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Describes one parameter a stand-in strategy takes, for a caller building a form from it.</summary>
public sealed class StandInParameterSchema
{
    /// <summary>Gets or sets the parameter's name, matching the request field the strategy reads.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter's type: "string", "integer", "number", "date" or "array".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the strategy refuses without this parameter.</summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets the closed set of values this parameter accepts, or null when any value of its type is accepted.</summary>
    public IReadOnlyList<string>? AllowedValues { get; set; }
}
