using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Carries a data set from the configuration of its stored kind to the configuration of another (FDW-6806): the one move between kinds, which
/// an update whose sources change the kind and the conversion to a Graph data set (FDW-6972) both use, never a second copy.
/// </summary>
/// <remarks>
/// Everything a data set is keeps its identity: the domain Id and name (so references by either still resolve), the fields with their Ids
/// (so their descriptions and notes follow), the sources with their mappings and transformation chains keyed by mapping Id, the key fields,
/// filters, caching, category, classification, owner, version and audit trail. A part the target kind has no place for is refused naming it,
/// never dropped: aggregates going to a joined kind, joins or a stated combination going to Simple. The provider's write of the moved
/// record retires the old kind's row in the same unit (DomainConfigurationProviderBase), so nothing is half moved.
/// </remarks>
public static class DataSetKindMove
{
    /// <summary>Builds the configuration of <paramref name="kind"/> that carries everything <paramref name="stored"/> carries.</summary>
    /// <param name="stored">The data set as it is stored, with the request's changes already merged.</param>
    /// <param name="kind">The kind the data set moves to: Simple, Compound or Federated.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The moved configuration, or the refusal: <c>AggregatesNotAllowed</c> for aggregates going to a joined kind, <c>JoinsNotAllowed</c> for
    /// joins or a stated combination going to Simple, <c>ImplementationInvalid</c> for a kind with no configuration here.
    /// </returns>
    public static IGenericResult<IDataSetImplementationConfiguration> To(IDataSetImplementationConfiguration stored, string kind, ILogger logger)
    {
        var aggregates = stored.AggregatesOrNone();
        var joins = stored.JoinsOrNone();
        var combination = stored.SourceCombinationOrNone();

        if (string.Equals(kind, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
        {
            return joins.Count > 0 || !string.IsNullOrWhiteSpace(combination)
                ? Refused("JoinsNotAllowed", stored, kind, logger)
                : Carried(stored, new SimpleDataSetConfiguration { Aggregates = [.. aggregates] }, kind);
        }

        if (aggregates.Count > 0)
            return Refused("AggregatesNotAllowed", stored, kind, logger);

        if (string.Equals(kind, CompoundDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            return Carried(stored, new CompoundDataSetConfiguration { Joins = [.. joins], SourceCombination = combination }, kind);

        if (string.Equals(kind, FederatedDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
        {
            return Carried(
                stored,
                new FederatedDataSetConfiguration
                {
                    Joins = [.. joins],
                    SourceCombination = combination,
                    FederationStrategy = stored.FederationStrategyOrNone(),
                    FederationOptions = [.. stored.FederationOptionsOrNone()],
                },
                kind);
        }

        return GenericResult<IDataSetImplementationConfiguration>.Failure(
            DataSetsResultCodes.ImplementationInvalid, logger, ImplementationInvalidCode.Describe(stored.Name, kind));
    }

    private static IGenericResult<IDataSetImplementationConfiguration> Refused(
        string code, IDataSetImplementationConfiguration stored, string kind, ILogger logger) =>
        GenericResult<IDataSetImplementationConfiguration>.Failure(
            DataSetsResultCodes.ByName(code), logger, ResultDetails.Create("name", stored.Name, "implementation", kind));

    // Every member a data set of any kind carries, onto the target; the conversion to a Graph data set (FDW-6972) carries it the same way.
    internal static IGenericResult<IDataSetImplementationConfiguration> Carried(
        IDataSetImplementationConfiguration stored, IDataSetImplementationConfiguration target, string kind)
    {
        target.Id = stored.Id;
        target.Name = stored.Name;
        target.Domain = stored.Domain;
        target.Implementation = kind;
        target.DisplayName = stored.DisplayName;
        target.Abbreviation = stored.Abbreviation;
        target.IsSortable = stored.IsSortable;
        target.IsFilterable = stored.IsFilterable;
        target.SourceDataSetName = stored.SourceDataSetName;
        target.Description = stored.Description;
        target.ClassificationId = stored.ClassificationId;
        target.OwnerUserId = stored.OwnerUserId;
        target.Version = stored.Version;
        target.CategoryId = stored.CategoryId;
        target.RecordTypeName = stored.RecordTypeName;
        target.Fields = stored.Fields;
        target.Sources = stored.Sources;
        target.KeyFields = stored.KeyFields;
        target.Filters = stored.Filters;
        target.Caching = stored.Caching;
        target.IsCurrent = stored.IsCurrent;
        target.IsDeleted = stored.IsDeleted;
        target.SrcCreateDate = stored.SrcCreateDate;
        target.CreateDate = stored.CreateDate;
        target.CreateBy = stored.CreateBy;
        target.CreateOnBehalfOf = stored.CreateOnBehalfOf;
        target.ModifyDate = stored.ModifyDate;
        target.ModifyBy = stored.ModifyBy;
        target.ModifyOnBehalfOf = stored.ModifyOnBehalfOf;
        return GenericResult<IDataSetImplementationConfiguration>.Success(target);
    }
}
