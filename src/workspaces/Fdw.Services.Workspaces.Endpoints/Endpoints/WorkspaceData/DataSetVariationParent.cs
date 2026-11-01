using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Resolves what a new view is a view of: a data set by name, or a view of the same workspace by name, whose root data set
/// it then shares (FDW-1032).
/// </summary>
/// <remarks>
/// A view's parent is a data set or another view, never anything else. A view of a view names its parent view; the data set
/// it varies is the parent's, so a request that names both must name the same one. The chain may not pass
/// <see cref="DataSetVariationChain.MaxDepth"/> views, the new one included.
/// </remarks>
internal static class DataSetVariationParent
{
    /// <summary>Resolves the base of the view a request describes.</summary>
    /// <param name="dataSets">The data set provider.</param>
    /// <param name="variations">The variation provider.</param>
    /// <param name="workspace">The workspace that will own the view.</param>
    /// <param name="request">The create request.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The root data set and parent, or the refusal.</returns>
    internal static async Task<IGenericResult<DataSetVariationBase>> Resolve(
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations,
        IWorkspaceImplementationConfiguration workspace,
        CreateDataSetVariationRequest request,
        ILogger logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ParentName))
        {
            var named = await dataSets.Get(request.BaseDataSetName, ct).ConfigureAwait(false);
            return Based(named, null, request.BaseDataSetName, logger);
        }

        var parent = await DataSetVariationQueries.Named(variations, workspace.Id, request.ParentName, ct).ConfigureAwait(false);
        if (parent.IsFailure) return parent.ToNewResult<DataSetVariationBase>();
        if (parent.Value is not { } row)
            return DataSetVariationChain.Invalid<DataSetVariationBase>(logger, request.Name, $"'{request.ParentName}' is not a view of workspace '{workspace.Name}'");

        var ancestors = await DataSetVariationChain.Ancestors(variations, row.ParentVariationId, request.ParentName, logger, ct).ConfigureAwait(false);
        if (ancestors.IsFailure) return ancestors.ToNewResult<DataSetVariationBase>();
        if (ancestors.Value!.Count + 2 > DataSetVariationChain.MaxDepth)
            return DataSetVariationChain.Invalid<DataSetVariationBase>(logger, request.Name, $"a chain of views holds at most {DataSetVariationChain.MaxDepth}, and this one would hold more");

        var main = await dataSets.Get(row.BaseDataSetId, ct).ConfigureAwait(false);
        var based = Based(main, row, request.ParentName, logger);
        if (based.IsFailure || string.IsNullOrWhiteSpace(request.BaseDataSetName)) return based;

        return string.Equals(based.Value!.Main.Name, request.BaseDataSetName, StringComparison.OrdinalIgnoreCase)
            ? based
            : DataSetVariationChain.Invalid<DataSetVariationBase>(logger, request.Name, $"baseDataSetName '{request.BaseDataSetName}' is not the data set of its parent '{request.ParentName}'");
    }

    private static IGenericResult<DataSetVariationBase> Based(
        IGenericResult<IDataSetImplementationConfiguration> main, DataSetVariationDomainConfiguration? parent, string named, ILogger logger)
    {
        if (main.IsFailure) return main.ToNewResult<DataSetVariationBase>();

        return main.Value is { } found
            ? GenericResult<DataSetVariationBase>.Success(new DataSetVariationBase(found, parent))
            : GenericResult<DataSetVariationBase>.Failure(
                WorkspacesResultCodes.ByName("DataSetNotVisible"), logger,
                ResultDetails.Create("dataSet", named));
    }
}
