using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>Reads what the dataflow graph layers draw from.</summary>
internal static class DataflowGraphInputsLoader
{
    /// <summary>
    /// Reads the data sets, datastores, sources and pipelines. A failed read of any of them fails the
    /// load: the graph is rooted at the pipelines, so an unread pipeline list would draw an empty
    /// graph and call it the answer.
    /// </summary>
    /// <remarks>
    /// When <paramref name="nameCategories"/> is true the DataSet category tree is read and the data sets the pipelines reach are
    /// labelled with their category path; a layer that draws no categories (the sources layer) states false and does not pay for the read.
    /// </remarks>
    public static async Task<IGenericResult<DataflowGraphInputs>> Load(
        DataflowGraphConfigurationProvider provider,
        IPipelineConfigurationProvider pipelineProvider,
        ILogger logger,
        bool nameCategories,
        CancellationToken cancellationToken = default)
    {
        DataflowGraphConfigurationProviderLog.LoadTrace(logger);

        var dataSets = await provider.LoadDataSets(cancellationToken).ConfigureAwait(false);
        if (!dataSets.IsSuccess)
            return dataSets.ToNewResult<DataflowGraphInputs>();

        var dataStores = await provider.LoadDataStores(cancellationToken).ConfigureAwait(false);
        if (!dataStores.IsSuccess)
            return dataStores.ToNewResult<DataflowGraphInputs>();

        var owners = await provider.LoadDataSetSources(cancellationToken).ConfigureAwait(false);
        if (!owners.IsSuccess)
            return owners.ToNewResult<DataflowGraphInputs>();

        var variations = await provider.LoadVariations(cancellationToken).ConfigureAwait(false);
        if (!variations.IsSuccess)
            return variations.ToNewResult<DataflowGraphInputs>();

        var pipelines = await PipelineLineageLoader.Read(pipelineProvider, logger, cancellationToken).ConfigureAwait(false);
        if (!pipelines.IsSuccess)
            return pipelines.ToNewResult<DataflowGraphInputs>();

        var paths = nameCategories
            ? await CategoryPaths(provider, DataflowReachability.Owners(pipelines.Value!, owners.Value!), logger, cancellationToken).ConfigureAwait(false)
            : GenericResult<IReadOnlyDictionary<Guid, string?>>.Success(new Dictionary<Guid, string?>());
        if (!paths.IsSuccess)
            return paths.ToNewResult<DataflowGraphInputs>();

        var inputs = new DataflowGraphInputs(dataSets.Value!, dataStores.Value!, owners.Value!, pipelines.Value!, variations.Value!, paths.Value!);
        DataflowGraphConfigurationProviderLog.Scoped(logger, inputs.Pipelines.Count, inputs.ReachableOwners.Count, dataSets.Value!.Count);
        return GenericResult<DataflowGraphInputs>.Success(inputs);
    }

    // The path of the category each drawn data set sits under, from the DataSet tree read once. A drawn data set that cites a category
    // that is not in the tree is a fault in the store: the load fails rather than draw the node as if it sat under none. A data set no
    // pipeline reaches is not drawn, so it is not checked.
    private static async Task<IGenericResult<IReadOnlyDictionary<Guid, string?>>> CategoryPaths(
        DataflowGraphConfigurationProvider provider,
        IReadOnlyList<IDataSetImplementationConfiguration> owners,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var tree = await provider.LoadDataSetCategories(cancellationToken).ConfigureAwait(false);
        if (!tree.IsSuccess || tree.Value is null)
            return tree.ToNewResult<IReadOnlyDictionary<Guid, string?>>();

        var paths = new Dictionary<Guid, string?>();
        foreach (var owner in owners)
        {
            var label = CategoryLinks.Label(tree.Value, DataSetCategoryConfiguration.ImplementationName, owner.CategoryId, logger);
            if (label.IsFailure || label.Value is null)
                return label.ToNewResult<IReadOnlyDictionary<Guid, string?>>();

            paths[owner.Id] = label.Value.Path;
        }

        return GenericResult<IReadOnlyDictionary<Guid, string?>>.Success(paths);
    }
}
