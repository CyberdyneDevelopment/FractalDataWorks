using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Builds the configuration of the kind a create request names: each kind of data set is its own implementation with
/// its own configuration class and table (FDW-1031).
/// </summary>
/// <remarks>
/// A Simple data set takes the request's aggregates, a Compound one its joins, a Federated one its joins and its
/// federation strategy. A kind with an option in <c>DataSetTypes</c> but no implementation here (one declared
/// outside FDW) is refused as an invalid implementation rather than given another kind's configuration.
/// </remarks>
internal static class DataSetConfigurationFactory
{
    /// <summary>Builds the configuration a create request describes.</summary>
    /// <param name="request">The create request; its implementation is already known to be a data set kind.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The configuration, or <c>ImplementationInvalid</c> when the kind has no implementation here.</returns>
    internal static IGenericResult<IDataSetImplementationConfiguration> Create(CreateDataSetRequest request, ILogger logger)
    {
        var kind = DataSetTypes.ByName(request.Implementation).Name;
        IDataSetImplementationConfiguration config;
        if (string.Equals(kind, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new SimpleDataSetConfiguration { Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates) };
        else if (string.Equals(kind, CompoundDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new CompoundDataSetConfiguration { Joins = DataSetQueryHelper.MapJoins(request.Joins) };
        else if (string.Equals(kind, FederatedDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new FederatedDataSetConfiguration
            {
                Joins = DataSetQueryHelper.MapJoins(request.Joins),
                FederationStrategy = request.FederationStrategy,
            };
        else
            return GenericResult<IDataSetImplementationConfiguration>.Failure(
                DataSetsResultCodes.ImplementationInvalid, logger,
                ImplementationInvalidCode.Describe(request.Name, request.Implementation));

        config.Name = request.Name;
        config.Implementation = request.Implementation;
        config.Description = request.Description ?? string.Empty;
        config.ClassificationId = request.ClassificationId;
        config.Category = request.Category ?? "Dataset";
        config.Version = request.Version;
        config.RecordTypeName = request.RecordTypeName;
        config.KeyFields = request.KeyFields
            .Select((name, i) => new DataSetKeyFieldConfiguration { KeyName = name, KeyType = "Surrogate", Ordinal = i })
            .ToList();
        config.SourceDataSetName = request.SourceDataSetName;
        config.Fields = DataSetQueryHelper.MapFields(request.Fields);
        // Nothing exists yet on create, so no source has a prior identity to keep.
        config.Sources = DataSetQueryHelper.MapSources(request.Sources, []);
        config.Caching = DataSetQueryHelper.MapCaching(request.Caching);
        return GenericResult<IDataSetImplementationConfiguration>.Success(config);
    }
}
