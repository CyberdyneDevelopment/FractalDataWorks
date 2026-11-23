using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Works out a data set's kind from its sources (FDW-6806): the one place create, update and the graph update ask, so no endpoint decides on
/// its own and no caller states a kind.
/// </summary>
/// <remarks>
/// One source (or none) is Simple. Several sources that all read one data store are Compound. Several sources across data stores are
/// Federated, and so is a set with a source of kind DataSet, which has no data store of its own and which a Compound query cannot read
/// (FDW-6811). "One data store" is judged by the stores' Ids, read through the data store configuration, never by comparing names. How the
/// sources combine (a join or a union, FDW-6946) does not change the kind.
/// </remarks>
internal static class DataSetKindDerivation
{
    /// <summary>The source kind of a source that is another data set.</summary>
    private const string DataSetSourceKind = "DataSet";

    /// <summary>Says whether a stored kind is one the sources make: Simple, Compound and Federated follow their sources; Graph and Variation do not (FDW-6129).</summary>
    /// <param name="implementation">The data set's stored kind, an implementation name.</param>
    /// <returns>True for a kind a change of sources can move; false for a kind that has its own routes and is never moved by an update of sources.</returns>
    internal static bool FollowsItsSources(string implementation) =>
        string.Equals(implementation, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal)
        || string.Equals(implementation, CompoundDataSetConfiguration.ImplementationName, StringComparison.Ordinal)
        || string.Equals(implementation, FederatedDataSetConfiguration.ImplementationName, StringComparison.Ordinal);

    /// <summary>Works out the kind of a data set from its sources.</summary>
    /// <param name="dataSetName">The data set's name, for a refusal.</param>
    /// <param name="sources">Each source's name, the data store it names and its source kind.</param>
    /// <param name="stores">Reads each named data store.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>
    /// The implementation name (Simple, Compound or Federated), or the refusal: <c>SourceDataStoreMissing</c> for a source of a store that names
    /// none, <c>SourceDataStoreNotFound</c> for a store that does not exist, or the store read's own failure.
    /// </returns>
    internal static async Task<IGenericResult<string>> Derive(
        string dataSetName,
        IReadOnlyList<(string Source, string? DataStoreName, string? SourceKind)> sources,
        IDataStoreConfigurationProvider stores,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (sources.Count <= 1)
            return GenericResult<string>.Success(SimpleDataSetConfiguration.ImplementationName);

        if (sources.Any(source => string.Equals(source.SourceKind, DataSetSourceKind, StringComparison.Ordinal)))
            return GenericResult<string>.Success(FederatedDataSetConfiguration.ImplementationName);

        var storeIds = new HashSet<Guid>();
        foreach (var source in sources)
        {
            if (string.IsNullOrWhiteSpace(source.DataStoreName))
                return GenericResult<string>.Failure(
                    DataSetsResultCodes.ByName("SourceDataStoreMissing"), logger,
                    ResultDetails.Create("name", dataSetName, "source", source.Source));

            var store = await stores.Get(source.DataStoreName, cancellationToken).ConfigureAwait(false);
            if (store.IsFailure)
                return store.ToNewResult<string>();
            if (store.Value is null)
                return GenericResult<string>.Failure(
                    DataSetsResultCodes.ByName("SourceDataStoreNotFound"), logger,
                    ResultDetails.Create("name", dataSetName, "source", source.Source, "dataStore", source.DataStoreName));

            storeIds.Add(store.Value.Id);
        }

        return GenericResult<string>.Success(storeIds.Count == 1
            ? CompoundDataSetConfiguration.ImplementationName
            : FederatedDataSetConfiguration.ImplementationName);
    }
}
