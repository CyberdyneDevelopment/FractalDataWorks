using System.Collections.Generic;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// A strand's request to claim a non-overlapping slice of the session's working copy so it can work
/// concurrently with other strands without conflict. The paths describe the files or directories the
/// strand intends to touch.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ScopeRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeRequest"/> class.
    /// </summary>
    /// <param name="strandId">The identifier of the strand requesting the scope.</param>
    /// <param name="paths">The repo-relative paths (files or directories) the strand intends to touch.</param>
    public ScopeRequest(string strandId, IReadOnlyList<string> paths)
    {
        StrandId = strandId;
        Paths = paths;
    }

    /// <summary>Gets the identifier of the strand requesting the scope.</summary>
    public string StrandId { get; }

    /// <summary>Gets the repo-relative paths (files or directories) the strand intends to touch.</summary>
    public IReadOnlyList<string> Paths { get; }
}
