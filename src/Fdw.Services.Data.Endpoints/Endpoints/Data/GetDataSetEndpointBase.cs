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
/// Delegates all reads to DataSetConfigurationProvider.
/// </summary>
public abstract class GetDataSetEndpointBase : CrudGetEndpointBase<DataSetNameRequest, DataSetDetailResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly CategoryConfigurationProvider _categories;

    /// <inheritdoc />
    protected GetDataSetEndpointBase(
        ILogger<GetDataSetEndpointBase> logger, DataSetConfigurationProvider dataSetProvider, CategoryConfigurationProvider categories) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
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

        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<DataSetDetailResponse?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null) return tree.ToNewResult<DataSetDetailResponse?>();

        var described = DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(result.Value), tree.Value, result.Value.CategoryId, Logger);
        return described.IsFailure
            ? described.ToNewResult<DataSetDetailResponse?>()
            : GenericResult<DataSetDetailResponse?>.Success(described.Value);
    }
}
