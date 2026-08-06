using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Data for organized using directives.
/// </summary>
public sealed class OrganizedUsingsData
{
    /// <summary>
    /// Gets or sets the number of using directives.
    /// </summary>
    public int UsingCount { get; set; }

    /// <summary>
    /// Gets or sets the list of organized using directives.
    /// </summary>
    public IReadOnlyList<UsingInfo> OrganizedUsings { get; set; } = System.Array.Empty<UsingInfo>();
}