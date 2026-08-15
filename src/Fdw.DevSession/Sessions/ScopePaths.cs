using System;
using System.Collections.Generic;

namespace Fdw.DevSession.Sessions;

/// <summary>Decides whether two sets of claimed paths overlap.</summary>
/// <remarks>
/// This is the whole basis of strand fencing, so it lives in its own type and is tested directly
/// rather than only through the coordinator.
/// </remarks>
internal static class ScopePaths
{
    /// <summary>Returns true when any path in <paramref name="left"/> collides with any in <paramref name="right"/>.</summary>
    internal static bool Overlap(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        foreach (var l in left)
        {
            foreach (var r in right)
            {
                if (Collide(Normalize(l), Normalize(r))) return true;
            }
        }
        return false;
    }

    /// <summary>Normalizes a claimed path for comparison.</summary>
    internal static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A claimed path cannot be empty.", nameof(path));
        return path.Replace('\\', '/').TrimEnd('/');
    }

    // Why OrdinalIgnoreCase: fencing must err toward REFUSING a claim. A false overlap costs one
    // caller a retry with a narrower scope; a missed overlap lets two strands edit the same file
    // and silently lose one side's work. On a case-insensitive filesystem "Foo.cs" and "foo.cs" are
    // the same file, so treating them as colliding everywhere is the safe direction.
    private static bool Collide(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            || IsUnder(left, right)
            || IsUnder(right, left);

    // Why the explicit "/" : a plain StartsWith would make "src/Foo" collide with "src/FooBar",
    // which are unrelated siblings. Containment only counts on a segment boundary.
    private static bool IsUnder(string candidate, string ancestor)
        => candidate.StartsWith(ancestor + "/", StringComparison.OrdinalIgnoreCase);
}
