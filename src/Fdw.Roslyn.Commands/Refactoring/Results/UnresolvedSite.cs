using System;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// Identifies a single <c>&lt;inheritdoc/&gt;</c> occurrence that Roslyn could not resolve,
/// so an explicit XML doc comment must be written there to satisfy MA0196.
/// </summary>
public sealed class UnresolvedSite
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnresolvedSite"/> class.
    /// </summary>
    /// <param name="filePath">Absolute path of the source file containing the site.</param>
    /// <param name="line">One-based line number of the <c>&lt;inheritdoc/&gt;</c> element.</param>
    /// <param name="column">One-based column number of the <c>&lt;inheritdoc/&gt;</c> element.</param>
    /// <param name="symbolDisplayName">Display name of the member the doc comment is attached to.</param>
    /// <param name="reason">Why the site could not be resolved.</param>
    public UnresolvedSite(string filePath, int line, int column, string symbolDisplayName, UnresolvedReason reason)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Line = line;
        Column = column;
        SymbolDisplayName = symbolDisplayName ?? throw new ArgumentNullException(nameof(symbolDisplayName));
        Reason = reason;
    }

    /// <summary>
    /// Gets the absolute path of the source file containing the site.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the one-based line number of the <c>&lt;inheritdoc/&gt;</c> element.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the one-based column number of the <c>&lt;inheritdoc/&gt;</c> element.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the display name of the member the doc comment is attached to.
    /// </summary>
    public string SymbolDisplayName { get; }

    /// <summary>
    /// Gets the reason the site could not be resolved.
    /// </summary>
    public UnresolvedReason Reason { get; }
}
