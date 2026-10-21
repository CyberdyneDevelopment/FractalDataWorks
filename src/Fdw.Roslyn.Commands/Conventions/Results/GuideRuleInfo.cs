using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// A must or must-not statement with its citation.
/// </summary>
public sealed class GuideRuleInfo
{
    /// <summary>Gets the statement.</summary>
    public required string Head { get; init; }

    /// <summary>Gets the explanation, as HTML.</summary>
    public required string Body { get; init; }

    /// <summary>Gets the source line the statement rests on.</summary>
    public required string Cite { get; init; }

    /// <summary>Gets the analyzer id that catalogs the rule; null when none does.</summary>
    public required string? Analyzer { get; init; }
}
