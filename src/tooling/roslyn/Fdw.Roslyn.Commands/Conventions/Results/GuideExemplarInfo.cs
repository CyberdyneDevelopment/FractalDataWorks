using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// A real implementation a guide names as the one to copy, with where it lives now.
/// </summary>
public sealed class GuideExemplarInfo
{
    /// <summary>Gets the exemplar's fully-qualified type name.</summary>
    public required string Type { get; init; }

    /// <summary>Gets what this exemplar is the example of.</summary>
    public required string Role { get; init; }

    /// <summary>Gets the citation the guide recorded, as path:line.</summary>
    public required string StampedCite { get; init; }

    /// <summary>Gets how the exemplar was found in the loaded solution.</summary>
    public required GuideExemplarResolution Resolution { get; init; }

    /// <summary>Gets the solution document that declares it; null unless resolved.</summary>
    public required string? FilePath { get; init; }

    /// <summary>Gets the line that declares it now; null unless resolved.</summary>
    public required int? Line { get; init; }

    /// <summary>Gets its XML summary; null when unresolved or undocumented.</summary>
    public required string? Summary { get; init; }
}
