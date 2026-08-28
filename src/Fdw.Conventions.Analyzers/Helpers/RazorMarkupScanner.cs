using System;
using System.Collections.Generic;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Classifies every offset of a .razor document as markup or non-markup with a single forward pass,
/// so a raw text scan can be restricted to the markup portions of the document.
/// </summary>
/// <remarks>
/// An analyzer only ever sees a .razor file as an <see cref="Microsoft.CodeAnalysis.AdditionalText"/> —
/// raw text, with no Razor parse tree and no semantic model — so markup and C# must be separated
/// textually. Non-markup is: <c>@* ... *@</c> Razor comments, and, inside <c>@code { }</c> /
/// <c>@functions { }</c> / <c>@{ }</c> blocks, the C# string and character literals and the
/// <c>//</c> and <c>/* */</c> comments.
/// <para>
/// Why the code block is not excluded wholesale: markup written inside a <c>RenderFragment</c> lambda
/// declared in <c>@code</c> is real markup and must still be reported. Excluding only literals and
/// comments inside the block reproduces a plain text search over the markup exactly, while still
/// keeping element text embedded in a C# string out of the results.
/// </para>
/// This is a heuristic, not a Razor parser. It does not model attribute quoting, transitions inside
/// expressions, or <c>&lt;text&gt;</c> regions.
/// </remarks>
internal sealed class RazorMarkupScanner
{
    private readonly List<(int Start, int End)> nonMarkupRegions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RazorMarkupScanner"/> class for a document.
    /// </summary>
    /// <param name="source">The full text of the .razor document.</param>
    internal RazorMarkupScanner(string source)
    {
        var i = 0;
        while (i < source.Length)
        {
            if (source[i] != '@')
            {
                i++;
                continue;
            }

            // @* razor comment *@
            if (i + 1 < source.Length && source[i + 1] == '*')
            {
                var commentEnd = source.IndexOf("*@", i + 2, StringComparison.Ordinal);
                commentEnd = commentEnd < 0 ? source.Length : commentEnd + 2;
                this.nonMarkupRegions.Add((i, commentEnd));
                i = commentEnd;
                continue;
            }

            // @code { ... } / @functions { ... } / @{ ... }
            if (IsBlockKeywordAt(source, i + 1, "code") ||
                IsBlockKeywordAt(source, i + 1, "functions") ||
                (i + 1 < source.Length && source[i + 1] == '{'))
            {
                var openBrace = source.IndexOf('{', i);
                if (openBrace < 0)
                    break;

                var blockEnd = FindMatchingBrace(source, openBrace);
                this.AddLiteralAndCommentSpans(source, openBrace, blockEnd);
                i = blockEnd;
                continue;
            }

            i++;
        }
    }

    /// <summary>
    /// Determines whether the given document offset lies in markup rather than in Razor comment text,
    /// a C# literal, or a C# comment.
    /// </summary>
    /// <param name="offset">The zero-based offset into the document text.</param>
    /// <returns><see langword="true"/> when the offset is markup; otherwise <see langword="false"/>.</returns>
    internal bool IsMarkup(int offset)
    {
        foreach (var region in this.nonMarkupRegions)
        {
            if (region.Start > offset)
                return true;

            if (offset < region.End)
                return false;
        }

        return true;
    }

    private void AddLiteralAndCommentSpans(string source, int start, int end)
    {
        for (var i = start; i < end && i < source.Length; i++)
        {
            var c = source[i];

            if (c == '"' || c == '\'')
            {
                var closingQuote = SkipLiteral(source, i);
                this.nonMarkupRegions.Add((i, closingQuote + 1));
                i = closingQuote;
                continue;
            }

            if (c != '/' || i + 1 >= source.Length)
                continue;

            if (source[i + 1] == '/')
            {
                var newLine = source.IndexOf('\n', i);
                newLine = newLine < 0 ? source.Length : newLine;
                this.nonMarkupRegions.Add((i, newLine));
                i = newLine;
            }
            else if (source[i + 1] == '*')
            {
                var commentEnd = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                commentEnd = commentEnd < 0 ? source.Length : commentEnd + 2;
                this.nonMarkupRegions.Add((i, commentEnd));
                i = commentEnd - 1;
            }
        }
    }

    private static bool IsBlockKeywordAt(string source, int index, string keyword)
    {
        if (index + keyword.Length > source.Length)
            return false;

        if (string.CompareOrdinal(source, index, keyword, 0, keyword.Length) != 0)
            return false;

        var afterKeyword = index + keyword.Length;
        while (afterKeyword < source.Length && char.IsWhiteSpace(source[afterKeyword]))
            afterKeyword++;

        return afterKeyword < source.Length && source[afterKeyword] == '{';
    }

    private static int FindMatchingBrace(string source, int openBrace)
    {
        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            var c = source[i];

            if (c == '"' || c == '\'')
            {
                i = SkipLiteral(source, i);
                continue;
            }

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                var newLine = source.IndexOf('\n', i);
                i = newLine < 0 ? source.Length : newLine;
                continue;
            }

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                var commentEnd = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = commentEnd < 0 ? source.Length : commentEnd + 1;
                continue;
            }

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}' && --depth == 0)
            {
                return i + 1;
            }
        }

        return source.Length;
    }

    private static int SkipLiteral(string source, int start)
    {
        var quote = source[start];
        var verbatim = start > 0 && source[start - 1] == '@' && quote == '"';

        for (var i = start + 1; i < source.Length; i++)
        {
            if (!verbatim && source[i] == '\\')
            {
                i++;
                continue;
            }

            if (source[i] != quote)
                continue;

            if (verbatim && i + 1 < source.Length && source[i + 1] == quote)
            {
                i++;
                continue;
            }

            return i;
        }

        return source.Length - 1;
    }
}
