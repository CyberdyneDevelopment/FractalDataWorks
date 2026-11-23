using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Builds the configuration of the kind the create request's sources make (FDW-6806, <see cref="DataSetKindDerivation"/>): each kind of data
/// set is its own implementation with its own configuration class and table (FDW-1031).
/// </summary>
/// <remarks>
/// A Simple data set takes the request's aggregates, a Compound one its joins, a Federated one its joins and its
/// federation strategy. A kind with an option in <c>DataSetTypes</c> but no implementation here (one declared
/// outside FDW) is refused as an invalid implementation rather than given another kind's configuration.
/// </remarks>
internal static class DataSetConfigurationFactory
{
    /// <summary>Builds the configuration a create request describes.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="kind">The kind the request's sources make, worked out by <see cref="DataSetKindDerivation"/>.</param>
    /// <param name="categoryId">The identity of the category the request names; null when it names none.</param>
    /// <param name="ownerUserId">The logical id of the calling user, who owns the new data set (FDW-4346).</param>
    /// <param name="values">Refuses a value the caller must choose and leaves out (FDW-3931).</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The configuration, <c>ImplementationInvalid</c> when the kind has no implementation here, the refusal naming a setting a value the
    /// request must state and leaves out (<c>ValueRequired</c>), or a refusal of its parts from <see cref="DataSetQueryHelper.ValidateSaved"/>.
    /// </returns>
    internal static IGenericResult<IDataSetImplementationConfiguration> Create(
        CreateDataSetRequest request, string kind, Guid? categoryId, Guid ownerUserId, DataSetRequestValues values, ILogger logger)
    {
        // The display name, before anything is built or written (FDW-6117).
        var named = DataSetNames.Check(request.DisplayName, request.Abbreviation, request.Name, logger);
        if (named.IsFailure) return named.ToNewResult<IDataSetImplementationConfiguration>();

        // A version the request leaves out is the kind's declared one (FDW-3956).
        var version = values.DeclaredText(request.Version, kind, DataSetDeclaredDefaults.Version, "version");
        if (version.IsFailure) return version.ToNewResult<IDataSetImplementationConfiguration>();

        var caching = DataSetQueryHelper.MapCaching(request.Caching, kind, values);
        if (caching.IsFailure) return caching.ToNewResult<IDataSetImplementationConfiguration>();

        // Nothing exists yet on create, so no source or field has a stored value to keep.
        var fields = DataSetQueryHelper.MapFields(request.Fields, [], values);
        if (fields.IsFailure) return fields.ToNewResult<IDataSetImplementationConfiguration>();

        var sources = DataSetQueryHelper.MapSources(request.Sources, [], values);
        if (sources.IsFailure) return sources.ToNewResult<IDataSetImplementationConfiguration>();

        IDataSetImplementationConfiguration config;
        if (string.Equals(kind, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new SimpleDataSetConfiguration { Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates) };
        else if (string.Equals(kind, CompoundDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new CompoundDataSetConfiguration
            {
                Joins = DataSetQueryHelper.MapJoins(request.Joins),
                SourceCombination = request.SourceCombination,
            };
        else if (string.Equals(kind, FederatedDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
            config = new FederatedDataSetConfiguration
            {
                Joins = DataSetQueryHelper.MapJoins(request.Joins),
                FederationStrategy = request.FederationStrategy,
                SourceCombination = request.SourceCombination,
                FederationOptions = DataSetQueryHelper.MapFederationOptions(request.FederationOptions),
            };
        else
            return GenericResult<IDataSetImplementationConfiguration>.Failure(
                DataSetsResultCodes.ImplementationInvalid, logger,
                ImplementationInvalidCode.Describe(request.Name, kind));

        config.Name = request.Name;
        config.Implementation = kind;
        config.Description = request.Description ?? string.Empty;
        DataSetNames.Set(config, request.DisplayName, request.Abbreviation);
        config.ClassificationId = request.ClassificationId;
        config.CategoryId = categoryId;
        config.OwnerUserId = ownerUserId;
        config.Version = version.Value!;
        config.RecordTypeName = request.RecordTypeName;
        config.KeyFields = request.KeyFields
            .Select((name, i) => new DataSetKeyFieldConfiguration { KeyName = name, KeyType = "Surrogate", Ordinal = i })
            .ToList();
        config.SourceDataSetName = request.SourceDataSetName;
        config.Fields = fields.Value!;
        config.Sources = sources.Value!;
        config.Caching = caching.Value;
        config.Filters = DataSetQueryHelper.MapFilters(request.Filters);

        // Joins name the data set's own sources, and a Compound data set's mappings name every column it reads (FDW-5022).
        if (DataSetQueryHelper.ValidateSaved(config, logger) is { IsFailure: true } refused)
            return refused.ToNewResult<IDataSetImplementationConfiguration>();

        return GenericResult<IDataSetImplementationConfiguration>.Success(config);
    }
}
