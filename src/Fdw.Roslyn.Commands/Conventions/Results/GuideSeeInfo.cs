using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// A pointer to where the rest of a subject is documented.
/// </summary>
public sealed class GuideSeeInfo
{
    /// <summary>Gets the document slug.</summary>
    public required string Doc { get; init; }

    /// <summary>Gets the page id within the document.</summary>
    public required string Page { get; init; }

    /// <summary>Gets what the reader finds there.</summary>
    public required string Why { get; init; }
}
