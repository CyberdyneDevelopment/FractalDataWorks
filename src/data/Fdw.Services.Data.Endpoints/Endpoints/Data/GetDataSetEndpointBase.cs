using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving a specific data set by name.
/// Resolves the name to a data set or a variation of one (a saved view) and maps it.
/// </summary>
public abstract class GetDataSetEndpointBase : CrudGetEndpointBase<DataSetNameRequest, DataSetDetailResponse>
{
    private readonly IDataSetResolver _resolver;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly IVariationChainAccess? _chainAccess;

    /// <summary>Initializes a new instance of the <see cref="GetDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="resolver">Resolves the name to a data set or a variation of one.</param>
    /// <param name="categories">Reads the category tree the data set sits in.</param>
    /// <param name="chainAccess">
    /// Decides whether the caller owns a variation (FDW-5446). A route built without one shows nobody the chain of a variation: its parent, the id of its root and its filters are left out.
    /// </param>
    protected GetDataSetEndpointBase(
        ILogger<GetDataSetEndpointBase> logger, IDataSetResolver resolver, ICategoryConfigurationProvider categories, IVariationChainAccess? chainAccess = null) : base(logger)
    {
        _resolver = resolver;
        _categories = categories;
        _chainAccess = chainAccess;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Finds a data set by name and maps it to a detail DTO.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        return LoadDataSetDetail(request.Name, ct);
    }

    /// <summary>
    /// Loads a data set by name from the provider and returns as detail DTO.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse?>> LoadDataSetDetail(string name, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, name, string.Empty);

        // A name may be a data set or a variation of one, such as a saved view; both answer here (FDW-2390).
        var result = await _resolver.Resolve(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<DataSetDetailResponse?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null) return tree.ToNewResult<DataSetDetailResponse?>();

        var described = DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(result.Value), tree.Value, result.Value.DataSet.CategoryId, Logger);
        if (described.IsFailure) return described.ToNewResult<DataSetDetailResponse?>();

        var shown = await ShowChainToOwner(_chainAccess, result.Value, described.Value!, ct).ConfigureAwait(false);
        return shown.IsFailure
            ? shown.ToNewResult<DataSetDetailResponse?>()
            : GenericResult<DataSetDetailResponse?>.Success(described.Value);
    }

    // A person who holds only a variation is told its name, kind and fields and nothing of what it varies (FDW-5446): the owner keeps the chain, everyone else loses it.
    internal static async Task<IGenericResult> ShowChainToOwner(
        IVariationChainAccess? chainAccess, ResolvedDataSet resolved, DataSetDetailResponse detail, CancellationToken ct)
    {
        if (resolved.Variation is not { } view) return GenericResult.Success();

        var owns = chainAccess is null ? GenericResult<bool>.Success(false) : await chainAccess.MayShowChain(view, ct).ConfigureAwait(false);
        if (owns.IsFailure) return owns;

        if (!owns.Value)
        {
            detail.ParentName = null;
            detail.BaseDataSetId = null;
            detail.ParentVariationId = null;
            detail.VariationFilters = null;
        }

        return GenericResult.Success();
    }
}
