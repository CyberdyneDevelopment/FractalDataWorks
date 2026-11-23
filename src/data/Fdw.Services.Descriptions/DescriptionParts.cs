using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>Splits a description into the parts it is stored in, and joins the parts back into the text that was written (FDW-6123).</summary>
/// <remarks>
/// <para>
/// A text no longer than the part length is one part. A longer one is cut into parts that, joined in order, are the text character for character: nothing is trimmed,
/// added or dropped, so what is read is exactly what was written.
/// </para>
/// <para>
/// Where the cut falls. Within a part length a text is cut BEFORE the last whitespace run that starts inside it, so a word is not broken. The whitespace stays with the
/// text and goes to the START of the next part, never the end of the one before it: no part but the last ends in whitespace, and the database's rule that a text is never
/// empty (<c>LEN</c> ignores trailing spaces) cannot refuse a part. A run longer than the part length with no whitespace in it is cut at the part length, in the middle of
/// the run. A cut is never made where what follows it is only whitespace: that tail stays on the part before, which is then longer than the part length by the whitespace,
/// because a part that is only whitespace would be refused by the database and a text whose last part is blank is the text written with its trailing blanks.
/// A part that would be only whitespace (a whitespace run as long as the part length) is lengthened to take the first character after the run.
/// </para>
/// <para>The part length is configuration (<c>DescriptionServiceTypes.PartLength</c>); nothing here chooses one.</para>
/// </remarks>
public static class DescriptionParts
{
    /// <summary>Splits a text into parts of about <paramref name="partLength"/> characters.</summary>
    /// <param name="text">The text, which has some character that is not whitespace.</param>
    /// <param name="partLength">The part length, at least 1.</param>
    /// <returns>The parts in reading order; one part when the text is within the part length.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="partLength"/> is below 1.</exception>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty or only whitespace, which no part may be.</exception>
    public static IReadOnlyList<string> Split(string text, int partLength)
    {
        if (partLength < 1)
            throw new ArgumentOutOfRangeException(nameof(partLength), partLength, "A part holds at least one character");
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("A text with nothing but whitespace has no parts", nameof(text));

        var parts = new List<string>();
        var start = 0;
        while (text.Length - start > partLength)
        {
            var cut = CutOf(text, start, partLength);
            if (cut is null)
                break;

            parts.Add(text.Substring(start, cut.Value - start));
            start = cut.Value;
        }

        parts.Add(text.Substring(start));
        return parts;
    }

    /// <summary>Joins the parts, given in any order with their ordinals, into the text they were split from.</summary>
    /// <param name="parts">The parts and their ordinals.</param>
    /// <returns>The text: the parts in Ordinal order, one after the other.</returns>
    public static string Join(IEnumerable<(int Ordinal, string Text)> parts)
    {
        var joined = new StringBuilder();
        foreach (var part in parts.OrderBy(part => part.Ordinal))
            joined.Append(part.Text);

        return joined.ToString();
    }

    /// <summary>Tells whether the ordinals are 1, 2, 3 ... n, each once.</summary>
    /// <param name="ordinals">The ordinals found, in any order.</param>
    /// <returns><see langword="true"/> when there is no gap and no repeat, and at least one part.</returns>
    public static bool AreContiguous(IEnumerable<int> ordinals)
    {
        var ordered = ordinals.OrderBy(ordinal => ordinal).ToList();
        return ordered.Count > 0 && ordered.Select((ordinal, index) => ordinal == index + 1).All(held => held);
    }

    /// <summary>The versions of a description from the versions written of each of its parts.</summary>
    /// <param name="written">Every version of every part: its Ordinal, text, when it was written, and whether it was a delete.</param>
    /// <returns>One version for each version of the first part, oldest first.</returns>
    /// <remarks>
    /// A version of the description is a version of its first part; the text it had is its first part joined with the latest version of every other part written before the
    /// next version of the first. A save writes every part in one unit, one after the other, so the parts of one save follow its first part and precede the next save's.
    /// </remarks>
    public static IReadOnlyList<DescriptionVersion> Versions(IReadOnlyCollection<(int Ordinal, string Text, DateTimeOffset At, bool Deleted)> written)
    {
        var firsts = written.Where(part => part.Ordinal == 1).OrderBy(part => part.At).ToList();
        var versions = new List<DescriptionVersion>(firsts.Count);
        for (var i = 0; i < firsts.Count; i++)
        {
            var next = i + 1 < firsts.Count ? firsts[i + 1].At : (DateTimeOffset?)null;
            var parts = written
                .Where(part => next is null || part.At < next)
                .GroupBy(part => part.Ordinal)
                .Select(group => group.OrderBy(part => part.At).Last())
                .Where(part => !part.Deleted)
                .Select(part => (part.Ordinal, part.Text));
            versions.Add(new DescriptionVersion(firsts[i].Deleted ? string.Empty : DescriptionParts.Join(parts), firsts[i].At, firsts[i].Deleted));
        }

        return versions;
    }

    // The index the next part starts at, or null when the rest is one part. The rest is longer than the part length here.
    private static int? CutOf(string text, int start, int partLength)
    {
        var limit = start + partLength;

        // Before the last whitespace run that starts in (start, limit]: the character before it is not whitespace, so the part has some, and the rest from the cut on must too.
        for (var i = limit; i > start; i--)
        {
            if (char.IsWhiteSpace(text[i]) && !char.IsWhiteSpace(text[i - 1]))
                return HasText(text, i) ? i : null;
        }

        // No whitespace to cut at: cut at the part length, after the first character that is not whitespace when the part so far is only whitespace.
        var cut = limit;
        if (IsBlank(text, start, cut))
        {
            cut = start;
            while (cut < text.Length && char.IsWhiteSpace(text[cut]))
                cut++;
            cut++;
        }

        // A cut does not fall between the two halves of one character outside the Basic Multilingual Plane.
        if (cut < text.Length && char.IsHighSurrogate(text[cut - 1]) && char.IsLowSurrogate(text[cut]))
            cut++;

        return cut < text.Length && HasText(text, cut) ? cut : null;
    }

    private static bool IsBlank(string text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return false;
        }

        return true;
    }

    private static bool HasText(string text, int from)
    {
        for (var i = from; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return true;
        }

        return false;
    }
}
