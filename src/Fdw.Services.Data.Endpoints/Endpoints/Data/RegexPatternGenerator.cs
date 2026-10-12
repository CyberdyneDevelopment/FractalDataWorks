using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generates a best-effort string matching a regular expression — literals, character classes
/// (<c>[abc]</c>, <c>[a-z]</c>, <c>\d</c>, <c>\w</c>, <c>\s</c>) and basic quantifiers
/// (<c>*</c>, <c>+</c>, <c>?</c>, <c>{n}</c>, <c>{n,m}</c>). Does not implement the full regex
/// grammar — no lookaround, backreferences, alternation groups or anchors — matching
/// RegexPatternStandInStrategyOption's own published limitation.
/// </summary>
internal static class RegexPatternGenerator
{
    private const int MaxUnboundedRepeat = 8;

    /// <summary>Generates one string honouring as much of <paramref name="pattern"/> as this generator supports.</summary>
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    public static string Generate(string pattern, Random random)
    {
        var result = new StringBuilder();
        var i = 0;
        while (i < pattern.Length)
        {
            var (chars, consumed) = ReadAtom(pattern, i, random);
            i += consumed;

            var (min, max, quantifierConsumed) = ReadQuantifier(pattern, i);
            i += quantifierConsumed;

            var repeat = min == max ? min : random.Next(min, max + 1);
            for (var r = 0; r < repeat; r++)
            {
                result.Append(chars(random));
            }
        }

        return result.ToString();
    }

    /// <summary>Reads one atom (a literal, an escape, or a bracket class) and returns a character generator for it.</summary>
    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static (Func<Random, char> Generator, int Consumed) ReadAtom(string pattern, int index, Random random)
    {
        var c = pattern[index];

        if (c == '\\' && index + 1 < pattern.Length)
        {
            var escaped = pattern[index + 1];
            return (escaped switch
            {
                'd' => r => (char)('0' + r.Next(10)),
                'w' => r => RandomFrom(r, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_"),
                's' => _ => ' ',
                _ => _ => escaped,
            }, 2);
        }

        if (c == '[')
        {
            var close = pattern.IndexOf(']', index + 1);
            if (close > index)
            {
                var classBody = pattern[(index + 1)..close];
                return (r => RandomFromClass(classBody, r), close - index + 1);
            }
        }

        if (c == '.')
        {
            return (r => RandomFrom(r, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"), 1);
        }

        return (_ => c, 1);
    }

    /// <summary>Reads a trailing quantifier (*, +, ?, {n}, {n,m}), defaulting to exactly one.</summary>
    private static (int Min, int Max, int Consumed) ReadQuantifier(string pattern, int index)
    {
        if (index >= pattern.Length) return (1, 1, 0);

        switch (pattern[index])
        {
            case '*': return (0, MaxUnboundedRepeat, 1);
            case '+': return (1, MaxUnboundedRepeat, 1);
            case '?': return (0, 1, 1);
            case '{':
                var close = pattern.IndexOf('}', index + 1);
                if (close > index)
                {
                    var body = pattern[(index + 1)..close];
                    var parts = body.Split(',');
                    var min = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    var max = parts.Length > 1 && parts[1].Length > 0
                        ? int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)
                        : (parts.Length > 1 ? min + MaxUnboundedRepeat : min);
                    return (min, max, close - index + 1);
                }
                return (1, 1, 0);
            default: return (1, 1, 0);
        }
    }

    [SuppressMessage("Security", "SCS0005:Weak random number generator", Justification = "Generates sample/preview data, not security-sensitive")]
    private static char RandomFrom(Random random, string pool) => pool[random.Next(pool.Length)];

    private static char RandomFromClass(string classBody, Random random)
    {
        var expanded = new StringBuilder();
        var i = 0;
        while (i < classBody.Length)
        {
            if (i + 2 < classBody.Length && classBody[i + 1] == '-')
            {
                for (var c = classBody[i]; c <= classBody[i + 2]; c++) expanded.Append(c);
                i += 3;
            }
            else
            {
                expanded.Append(classBody[i]);
                i++;
            }
        }

        return expanded.Length == 0 ? '?' : RandomFrom(random, expanded.ToString());
    }
}
