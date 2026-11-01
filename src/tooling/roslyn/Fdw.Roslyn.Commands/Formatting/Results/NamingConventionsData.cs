using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Data for naming conventions analysis.
/// </summary>
public sealed class NamingConventionsData
{
    /// <summary>
    /// Gets or sets the list of naming violations.
    /// </summary>
    public IReadOnlyList<NamingViolation> Violations { get; set; } = System.Array.Empty<NamingViolation>();
}