using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Counts variations by kind for each data set they vary: the tokens a data set node carries.</summary>
internal static class MapVariationCounts
{
    /// <summary>Groups variation rows by the data set they vary and then by kind.</summary>
    /// <param name="variations">The variation rows.</param>
    /// <returns>For each data set's logical id, one count per kind present, in kind order.</returns>
    public static IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> Of(
        IReadOnlyList<DataSetVariationDomainConfiguration> variations)
        => variations
            .GroupBy(variation => variation.BaseDataSetId)
            .ToDictionary(
                byDataSet => byDataSet.Key,
                byDataSet => (IReadOnlyList<WorkspaceMapVariationCountDto>)byDataSet
                    .GroupBy(variation => variation.Implementation, StringComparer.Ordinal)
                    .OrderBy(byKind => byKind.Key, StringComparer.Ordinal)
                    // Implementation is nullable on every domain row, but data.DataSetVariation.Implementation is NOT NULL:
                    // a variation always carries its kind.
                    .Select(byKind => new WorkspaceMapVariationCountDto { Kind = byKind.Key!, Count = byKind.Count() })
                    .ToList());
}
