using System.Text.RegularExpressions;
using System.Xml.Linq;
using System;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Reads the XML documentation of a symbol.
/// </summary>
internal static class GuideDocumentation
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets the documentation comment of a symbol, verbatim.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The comment XML, or null when the symbol carries none.</returns>
    public static string? Verbatim(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        return string.IsNullOrWhiteSpace(xml) ? null : xml;
    }

    /// <summary>
    /// Gets the summary of a symbol with its whitespace collapsed.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The summary text, or null when the symbol has no summary.</returns>
    public static string? Summary(ISymbol symbol)
    {
        var xml = Verbatim(symbol);
        if (xml is null) return null;

        var summary = XElement.Parse(xml).Element("summary");
        if (summary is null) return null;

        return Regex.Replace(summary.Value, "\\s+", " ", RegexOptions.CultureInvariant, MatchTimeout).Trim();
    }
}
