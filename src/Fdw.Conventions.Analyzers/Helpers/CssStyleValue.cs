using System;
using System.Collections.Generic;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Reads the declarations out of a markup style attribute value, including the string literals of a
/// Razor expression that selects between written-out styles.
/// </summary>
/// <remarks>
/// The reader is textual — a .razor document reaches an analyzer as raw text — so it stays inside what
/// text can settle. A declaration that carries a Razor transition (<c>width:@(x)px</c>) is not
/// returned: its value is decided at runtime, and the analyzer cannot see what it becomes.
/// </remarks>
internal static class CssStyleValue
{
    /// <summary>
    /// Collects the literal declarations of the attribute value that begins at <paramref name="valueStart"/>.
    /// </summary>
    /// <param name="text">The full document text.</param>
    /// <param name="valueStart">Index of the first character after the attribute's opening quote.</param>
    /// <param name="declarations">The list the declarations are appended to.</param>
    internal static void Collect(string text, int valueStart, List<CssDeclarationSpan> declarations)
    {
        var valueEnd = RazorAttributeValue.FindValueEnd(text, valueStart);
        if (valueEnd <= valueStart)
            return;

        // Why an expression value is read through its string literals: `@(ok ? "color:var(--a);" :
        // "color:var(--b);")` states both of its outcomes in the markup, and each is an ordinary
        // declaration list. Reading the expression as one run of text instead would parse `ok ? "color`
        // as a property name and settle nothing.
        if (text[valueStart] == '@')
        {
            CollectFromExpression(text, valueStart, valueEnd, declarations);
            return;
        }

        CollectFromRun(text, valueStart, valueEnd, declarations);
    }

    private static void CollectFromExpression(string text, int start, int end, List<CssDeclarationSpan> declarations)
    {
        for (var i = start; i < end; i++)
        {
            if (text[i] != '"')
                continue;

            var literalEnd = RazorAttributeValue.FindStringLiteralEnd(text, i);
            CollectFromRun(text, i + 1, Math.Min(literalEnd - 1, end), declarations);
            i = literalEnd - 1;
        }
    }

    /// <summary>
    /// Splits a plain run of declaration text on <c>;</c> and records each well-formed declaration.
    /// </summary>
    private static void CollectFromRun(string text, int start, int end, List<CssDeclarationSpan> declarations)
    {
        var i = start;

        while (i < end)
        {
            while (i < end && (char.IsWhiteSpace(text[i]) || text[i] == ';'))
                i++;

            var declarationStart = i;

            while (i < end && text[i] != ';')
                i++;

            var declarationEnd = i;
            while (declarationEnd > declarationStart && char.IsWhiteSpace(text[declarationEnd - 1]))
                declarationEnd--;

            if (declarationEnd > declarationStart)
                AddDeclaration(text, declarationStart, declarationEnd, declarations);
        }
    }

    private static void AddDeclaration(string text, int start, int end, List<CssDeclarationSpan> declarations)
    {
        var colon = -1;

        for (var i = start; i < end; i++)
        {
            // Why a transition disqualifies the whole declaration and not just its value: the property
            // name can be the computed part too, and either way there is no written value to judge.
            if (text[i] == '@')
                return;

            if (text[i] == ':' && colon < 0)
                colon = i;
        }

        if (colon <= start || colon >= end - 1)
            return;

        for (var i = start; i < colon; i++)
        {
            var c = text[i];
            if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '-')
                return;
        }

        var value = text.Substring(colon + 1, end - colon - 1).Trim();
        if (value.Length == 0)
            return;

        declarations.Add(new CssDeclarationSpan(
            start,
            end - start,
            text.Substring(start, colon - start).ToLowerInvariant(),
            value));
    }
}
