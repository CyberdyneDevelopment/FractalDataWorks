using System;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Reads the value of a markup attribute whose opening <c>name="</c> has already been located, and
/// classifies whether that value is decided by data at runtime.
/// </summary>
/// <remarks>
/// <para>
/// A Razor attribute value is not simply "everything up to the next double quote". An expression
/// value carries its own string literals — <c>style="@(ok ? "a" : "b")"</c> — so scanning for the
/// next quote stops in the middle of the expression. The reader therefore tracks Razor's own
/// nesting: parentheses, braces, and string literals, including the interpolated and verbatim forms.
/// </para>
/// <para>
/// The classification exists because "move it to a CSS class" is not always advice that can be
/// followed. A class names a fixed set of declarations. When the value carries a number the markup
/// computed — a grid coordinate, a percentage, a depth in pixels — there is no fixed set to name,
/// and the only way to express it in CSS would be to emit a class per possible value. Selecting
/// between two written-out literals is the opposite case: the alternatives are already fixed, so
/// they are two classes and a conditional on the class attribute.
/// </para>
/// </remarks>
internal static class RazorAttributeValue
{
    /// <summary>
    /// Reads the attribute value beginning at <paramref name="start"/> — the first character after
    /// the opening quote — and reports whether its content is computed from data.
    /// </summary>
    /// <param name="text">The full document text.</param>
    /// <param name="start">Index of the first character of the value.</param>
    /// <returns>
    /// <see langword="true"/> when the value is a Razor expression whose result depends on data the
    /// markup computed, so no fixed CSS class could carry it.
    /// </returns>
    public static bool IsDataDriven(string text, int start)
    {
        if (text is null || start < 0 || start >= text.Length || text[start] != '@')
            return false;

        var end = FindExpressionEnd(text, start);
        if (end <= start)
            return false;

        var expression = text.Substring(start, end - start);

        // An interpolated string is the direct evidence: a hole in the value means the value is not
        // one of a fixed set. `@($"width:{pct}%")` cannot be a class; `@(ok ? "x" : "y")` can.
        if (expression.IndexOf("$\"", StringComparison.Ordinal) >= 0)
            return true;

        // A bare invocation delegates the decision to code — `@CellStyle(placement)`. The analyzer
        // reads markup, not the method body, so it cannot see whether the result is computed. It is
        // treated as computed: reporting it would be advice the author cannot act on from here, and
        // the call itself is the author saying the value is derived.
        return IsInvocation(expression);
    }

    /// <summary>
    /// Finds the index one past the end of the Razor expression starting at <paramref name="start"/>.
    /// </summary>
    private static int FindExpressionEnd(string text, int start)
    {
        var i = start + 1; // past '@'
        if (i >= text.Length)
            return start;

        // @(...) — an explicit expression. Balance the parentheses, ignoring anything inside a string.
        if (text[i] == '(')
            return SkipBalanced(text, i, '(', ')');

        // @Identifier, optionally followed by member access and an argument list.
        while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.'))
            i++;

        if (i < text.Length && text[i] == '(')
            i = SkipBalanced(text, i, '(', ')');

        return i;
    }

    /// <summary>
    /// Returns the index one past the matching close delimiter, skipping over string literals.
    /// </summary>
    private static int SkipBalanced(string text, int open, char openChar, char closeChar)
    {
        var depth = 0;
        var i = open;

        while (i < text.Length)
        {
            var c = text[i];

            if (c == '"')
            {
                i = SkipStringLiteral(text, i);
                continue;
            }

            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                    return i + 1;
            }

            i++;
        }

        // Unbalanced — the markup is malformed or the expression runs past the document. Treat the
        // rest as the expression rather than guessing a shorter one.
        return text.Length;
    }

    /// <summary>
    /// Returns the index one past the closing quote of the string literal starting at
    /// <paramref name="quote"/>, honouring escapes and the verbatim form.
    /// </summary>
    private static int SkipStringLiteral(string text, int quote)
    {
        // Verbatim (@"..." or $@"...") — a doubled quote is an escaped quote, backslash is literal.
        var verbatim = quote > 0 && (text[quote - 1] == '@');

        var i = quote + 1;
        while (i < text.Length)
        {
            if (text[i] == '\\' && !verbatim)
            {
                i += 2;
                continue;
            }

            if (text[i] == '"')
            {
                if (verbatim && i + 1 < text.Length && text[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return text.Length;
    }

    /// <summary>
    /// Determines whether the expression is a method invocation rather than a parenthesised value.
    /// </summary>
    private static bool IsInvocation(string expression)
    {
        // Skip '@'. An explicit expression opens immediately with '(' and is not an invocation by
        // this test even if it contains one — `@(Foo())` states its own value, so it is judged on
        // its content like any other expression.
        var i = 1;
        if (i >= expression.Length || expression[i] == '(')
            return false;

        var identifierStart = i;
        while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == '.'))
            i++;

        return i > identifierStart && i < expression.Length && expression[i] == '(';
    }
}
