using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The rules of how a Compound or Federated data set's sources combine (FDW-6946): a data set with more than one source states a
/// <see cref="DataSetCompositionTypes"/> member that takes several sources (Join or Union), never none and never one its joins contradict,
/// and every source of a union maps every field.
/// </summary>
/// <remarks>
/// Checked when the data set is created or updated and again before it is read, through <see cref="JoinedDataSetRules"/>, and by the
/// runtime builder. Nothing is worked out from whether the data set has joins: a data set that states none is refused, never read as a
/// join or stacked as a union. Whether a combination is made of joins is the option's own <see cref="IDataSetCompositionType.RequiresJoins"/>.
/// </remarks>
public static class SourceCombinationRules
{
    /// <summary>Gets the composition a data set states.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <returns>The option the data set names, ignoring case; <see cref="DataSetCompositionTypes.NotFound"/> when it names none or a name no option has.</returns>
    public static IDataSetCompositionType Stated(IJoinedDataSetConfiguration dataSet) =>
        dataSet.SourceCombination is { } name && !string.IsNullOrWhiteSpace(name) ? Named(name.Trim()) : DataSetCompositionTypes.NotFound;

    // The option carrying this name, ignoring case, or NotFound.
    private static IDataSetCompositionType Named(string name)
    {
        var match = DataSetCompositionTypes.All().FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase));
        return match is null ? DataSetCompositionTypes.NotFound : match;
    }

    /// <summary>Gets whether a data set states a union: a composition of several sources that is not made of joins.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <returns>True when it states such a composition.</returns>
    public static bool IsUnion(IJoinedDataSetConfiguration dataSet)
    {
        var stated = Stated(dataSet);
        return !ReferenceEquals(stated, DataSetCompositionTypes.NotFound) && stated.AllowsMultipleSources && !stated.RequiresJoins;
    }

    /// <summary>Checks the data set's stated composition against its sources, joins and fields.</summary>
    /// <param name="dataSet">The data set, as it will be saved or as it was read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// Success, or the first refusal: <c>SourceCombinationMissing</c> for more than one source and no statement, <c>SourceCombinationNotFound</c>
    /// for a name no option has or one that takes a single source, <c>SourceCombinationJoinsMismatch</c> for a join with no joins or a union
    /// with joins, and <c>UnionFieldNotMapped</c> for a field a source of a union does not map.
    /// </returns>
    public static IGenericResult Validate(IJoinedDataSetConfiguration dataSet, ILogger logger)
    {
        var combinations = string.Join(", ", DataSetCompositionTypes.All().Where(option => option.AllowsMultipleSources).Select(option => option.Name));
        if (string.IsNullOrWhiteSpace(dataSet.SourceCombination))
        {
            return dataSet.Sources.Count > 1
                ? GenericResult.Failure(
                    DataSetsResultCodes.ByName("SourceCombinationMissing"), logger,
                    ResultDetails.Create("name", dataSet.Name, "sourceCount", dataSet.Sources.Count, "combinations", combinations))
                : GenericResult.Success();
        }

        var stated = Stated(dataSet);
        if (ReferenceEquals(stated, DataSetCompositionTypes.NotFound) || !stated.AllowsMultipleSources)
            return GenericResult.Failure(
                DataSetsResultCodes.ByName("SourceCombinationNotFound"), logger,
                ResultDetails.Create("name", dataSet.Name, "combination", dataSet.SourceCombination, "combinations", combinations));

        if (stated.RequiresJoins != (dataSet.Joins.Count > 0))
            return GenericResult.Failure(
                DataSetsResultCodes.ByName("SourceCombinationJoinsMismatch"), logger,
                ResultDetails.Create("name", dataSet.Name, "combination", stated.Name, "joinCount", dataSet.Joins.Count));

        return stated.RequiresJoins ? GenericResult.Success() : UnionFields(dataSet, logger);
    }

    // Every source of a union maps every stored field: the rows are stacked in the data set's field order, so a field one source leaves out
    // would read as null for its rows. Calculated fields have no column; they are applied after the read.
    private static IGenericResult UnionFields(IJoinedDataSetConfiguration dataSet, ILogger logger)
    {
        foreach (var field in dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted && !f.IsCalculated))
        {
            var unmapped = dataSet.Sources.FirstOrDefault(source => !source.FieldMappings.ContainsKey(field.Name));
            if (unmapped is not null)
                return GenericResult.Failure(
                    DataSetsResultCodes.ByName("UnionFieldNotMapped"), logger,
                    ResultDetails.Create("name", dataSet.Name, "source", unmapped.SourceName, "field", field.Name));
        }

        return GenericResult.Success();
    }
}
