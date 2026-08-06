using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// Report produced by the ResolveInheritDoc command: counts of <c>&lt;inheritdoc/&gt;</c> sites that
/// were expanded in place, plus the list of sites Roslyn could not resolve (the true MA0196 candidates).
/// </summary>
public sealed class ResolveInheritDocResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveInheritDocResult"/> class.
    /// </summary>
    /// <param name="filesScanned">Number of source documents inspected.</param>
    /// <param name="filesModified">Number of documents that had at least one site expanded.</param>
    /// <param name="sitesResolved">Number of <c>&lt;inheritdoc/&gt;</c> sites successfully expanded.</param>
    /// <param name="sitesUnresolved">Number of <c>&lt;inheritdoc/&gt;</c> sites that could not be resolved.</param>
    /// <param name="unresolved">The unresolved sites — true MA0196 candidates.</param>
    public ResolveInheritDocResult(
        int filesScanned,
        int filesModified,
        int sitesResolved,
        int sitesUnresolved,
        IReadOnlyList<UnresolvedSite> unresolved)
    {
        FilesScanned = filesScanned;
        FilesModified = filesModified;
        SitesResolved = sitesResolved;
        SitesUnresolved = sitesUnresolved;
        Unresolved = unresolved ?? throw new ArgumentNullException(nameof(unresolved));
    }

    /// <summary>
    /// Gets the number of source documents inspected.
    /// </summary>
    public int FilesScanned { get; }

    /// <summary>
    /// Gets the number of documents that had at least one site expanded.
    /// </summary>
    public int FilesModified { get; }

    /// <summary>
    /// Gets the number of <c>&lt;inheritdoc/&gt;</c> sites successfully expanded in place.
    /// </summary>
    public int SitesResolved { get; }

    /// <summary>
    /// Gets the number of <c>&lt;inheritdoc/&gt;</c> sites that could not be resolved.
    /// </summary>
    public int SitesUnresolved { get; }

    /// <summary>
    /// Gets the unresolved sites — the true MA0196 candidates that need explicit documentation.
    /// </summary>
    public IReadOnlyList<UnresolvedSite> Unresolved { get; }
}
