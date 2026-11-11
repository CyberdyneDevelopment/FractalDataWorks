using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The rules every data set that joins its sources (Compound, Federated) keeps: each join names two of the data set's own sources (FDW-5022),
/// and states a join type that joins (FDW-5099).
/// </summary>
/// <remarks>
/// Checked when the data set is created or updated and again before a Compound or Federated one is read, so a join to nothing, or by a type
/// that is missing, unknown or joins nothing, is never stored or run. There is no default join type: a join that states none is refused,
/// never run as Inner.
/// </remarks>
public static class JoinedDataSetRules
{
    /// <summary>Checks that every join's left and right source is one of the data set's sources, and that its join type joins.</summary>
    /// <param name="dataSet">The data set, as it will be saved or as it was read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// Success, or the first refusal: <c>JoinSourceNotInDataSet</c> naming a source that is not one of the data set's, <c>JoinTypeMissing</c>
    /// for a join with no type, <c>JoinTypeNotFound</c> for a type no <see cref="JoinTypes"/> option is named, or <c>JoinTypeNotSupported</c>
    /// for an option that joins nothing (None).
    /// </returns>
    public static IGenericResult Validate(IJoinedDataSetConfiguration dataSet, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(logger);

        var sources = dataSet.Sources.Select(s => s.SourceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var join in dataSet.Joins)
        {
            var outside = !sources.Contains(join.LeftSourceName) ? join.LeftSourceName
                : !sources.Contains(join.RightSourceName) ? join.RightSourceName
                : null;
            if (outside is not null)
                return GenericResult.Failure(
                    DataSetsResultCodes.ByName("JoinSourceNotInDataSet"), logger,
                    ResultDetails.Create("name", dataSet.Name, "ordinal", join.Ordinal, "source", outside)
                        .With("sources", string.Join(", ", dataSet.Sources.Select(s => s.SourceName))));

            if (JoinTypeRefusal(join.JoinType) is { } refusal)
                return GenericResult.Failure(
                    DataSetsResultCodes.ByName(refusal), logger,
                    ResultDetails.Create("name", dataSet.Name, "ordinal", join.Ordinal, "joinType", join.JoinType)
                        .With("joinTypes", string.Join(", ", JoinTypes.All().Where(StatedJoinType.Joins).Select(kind => kind.Name))));
        }

        return GenericResult.Success();
    }

    // The code refusing a join's type, or null when the type joins: on a condition, or every row of both sides (FDW-5099).
    private static string? JoinTypeRefusal(string? joinType)
    {
        if (string.IsNullOrWhiteSpace(joinType))
            return "JoinTypeMissing";

        var kind = StatedJoinType.Of(joinType);
        if (ReferenceEquals(kind, JoinTypes.NotFound))
            return "JoinTypeNotFound";
        return StatedJoinType.Joins(kind) ? null : "JoinTypeNotSupported";
    }
}
