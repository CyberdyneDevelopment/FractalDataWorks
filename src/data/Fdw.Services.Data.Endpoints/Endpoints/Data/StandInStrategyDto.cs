using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One stand-in strategy, as returned by the strategies list.</summary>
public class StandInStrategyDto
{
    /// <summary>Gets or sets the strategy's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameters this strategy takes.</summary>
    public IReadOnlyList<StandInParameterSchema> Parameters { get; set; } = [];

    /// <summary>Gets or sets what's simplified about this strategy's value generation, or null.</summary>
    public string? Limitations { get; set; }
}
