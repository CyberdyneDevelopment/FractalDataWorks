using System.Threading;
using System.Threading.Tasks;
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
    public static async Task<IGenericResult<DataflowGraphInputs>> Load(
        DataflowGraphConfigurationProvider provider,
        IPipelineConfigurationProvider pipelineProvider,
        ILogger logger,
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

        var inputs = new DataflowGraphInputs(dataSets.Value!, dataStores.Value!, owners.Value!, pipelines.Value!, variations.Value!);
        DataflowGraphConfigurationProviderLog.Scoped(logger, inputs.Pipelines.Count, inputs.ReachableOwners.Count, dataSets.Value!.Count);
        return GenericResult<DataflowGraphInputs>.Success(inputs);
    }
}
