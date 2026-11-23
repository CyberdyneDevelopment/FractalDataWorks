using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Descriptions.Abstractions;

namespace Fdw.Services.Data.DataSets;

/// <summary>
/// Puts the stored versions of a description beside the stored versions of the thing it describes, so a history that lists the versions of the thing shows the
/// text each one had (FDW-6208).
/// </summary>
/// <remarks>
/// The text of a version is the last version of the description written before the thing's next version: a save writes the description just after the
/// thing's own row, in the same unit, so the description of that save belongs to the version it follows and not to the one before it. The last version of the
/// thing has the last description. A version of the description that marks it removed, and a thing that never had one, read as no text; nothing stands in for it.
/// </remarks>
public static class DescriptionHistory
{
    /// <summary>Finds the text each version of a thing had.</summary>
    /// <param name="versionTimes">When each version of the thing was written, oldest first.</param>
    /// <param name="descriptions">The stored versions of the thing's description, oldest first.</param>
    /// <returns>The text of each version, in the order of <paramref name="versionTimes"/>.</returns>
    public static IReadOnlyList<string> TextsOf(IReadOnlyList<DateTimeOffset> versionTimes, IReadOnlyList<DescriptionVersion> descriptions)
    {
        var texts = new List<string>(versionTimes.Count);
        for (var i = 0; i < versionTimes.Count; i++)
        {
            var next = i + 1 < versionTimes.Count ? versionTimes[i + 1] : (DateTimeOffset?)null;
            var held = descriptions.LastOrDefault(description => next is null || description.WrittenAt < next);
            texts.Add(held is null || held.IsDeleted ? string.Empty : held.Text);
        }

        return texts;
    }
}
