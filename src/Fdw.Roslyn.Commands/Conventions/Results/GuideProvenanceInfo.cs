using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// The commit a guide document was verified against.
/// </summary>
public sealed class GuideProvenanceInfo
{
    /// <summary>Gets the repository the guide was verified against.</summary>
    public required string Repo { get; init; }

    /// <summary>Gets the branch the commit was resolved on; null when the stamp names none.</summary>
    public required string? Ref { get; init; }

    /// <summary>Gets the commit the guide was verified against.</summary>
    public required string Commit { get; init; }

    /// <summary>Gets the date the guide was verified.</summary>
    public required string Verified { get; init; }
}
