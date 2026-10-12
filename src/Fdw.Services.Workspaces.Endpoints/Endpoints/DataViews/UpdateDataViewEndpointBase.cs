using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Charts;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Changes a data view: <c>PATCH /data-views/{Name}</c> (FDW-1032).</summary>
/// <remarks>
/// Only the user who saved a view may change it. The change is checked the way a create is -- the chart must still be
/// drawable and the filter must still resolve against the view's root data set -- before anything is written, so a refusal
/// leaves the view as it was read. The data set and the parent view are not changeable.
/// </remarks>
public abstract class UpdateDataViewEndpointBase : CrudUpdateEndpointBase<UpdateDataViewRequest, DataViewResponse>
{
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected UpdateDataViewEndpointBase(
        ILogger<UpdateDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "data-views";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateDataViewRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataViewResponse?>> FindForUpdate(UpdateDataViewRequest request, CancellationToken ct)
    {
        var found = await DataViewStore.Find(_variations, request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<DataViewResponse?>();
        if (found.Value is not { } view) return GenericResult<DataViewResponse?>.Success(null);

        var parentName = await DataViewStore.ParentName(_variations, view, Logger, ct).ConfigureAwait(false);
        return parentName.IsFailure
            ? parentName.ToNewResult<DataViewResponse?>()
            : GenericResult<DataViewResponse?>.Success(DataViewResponseMapper.ToResponse(view, parentName.Value));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataViewResponse>> Update(
        UpdateDataViewRequest request, DataViewResponse existing, CancellationToken ct)
    {
        var found = await DataViewStore.Find(_variations, request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found.ToNewResult<DataViewResponse>();
        if (found.Value is not { } view)
        {
            return GenericResult<DataViewResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var owned = DataViewStore.OwnedByCaller(_authContext, view, Logger);
        if (owned.IsFailure) return owned.ToNewResult<DataViewResponse>();

        var refused = await Refusal(request, view, ct).ConfigureAwait(false);
        if (refused is not null) return refused;

        // After the checks, because the loaded view is the cached instance: a refusal must leave it as it was read.
        Apply(request, view);
        var saved = await _variations.Save(
            view, "DataSetVariation", DataViewDataSetVariationConfiguration.ImplementationName, view.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<DataViewResponse>();

        return GenericResult<DataViewResponse>.Success(DataViewResponseMapper.ToResponse(view, existing.ParentName));
    }

    /// <summary>The refusal a change that would leave the chart undrawable or the filter unresolvable is answered with, or null.</summary>
    private async Task<IGenericResult<DataViewResponse>?> Refusal(
        UpdateDataViewRequest request, IDataViewDataSetVariationConfiguration view, CancellationToken ct)
    {
        var refusals = new DataViewRefusals(view.Name, Logger, "DataViewSpecInvalid");
        var main = await _dataSets.Get(view.BaseDataSetId, ct).ConfigureAwait(false);
        if (main.IsFailure) return main.ToNewResult<DataViewResponse>();
        if (main.Value is not { } dataSet)
            return refusals.Invalid<DataViewResponse>($"the data set {view.BaseDataSetId} this view reads no longer exists");

        var fields = dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted).ToList();
        var reason = ChartSpecificationCheck.Reason(
            request.ChartType is null ? view.ChartType : request.ChartType,
            request.Encoding is null ? view.Encoding : request.Encoding,
            fields);
        if (reason is not null) return refusals.Invalid<DataViewResponse>(reason);

        var filter = DataViewFilter.Translate(Filters(request, view), fields, refusals);
        return filter.IsFailure ? filter.ToNewResult<DataViewResponse>() : null;
    }

    // The filter text the view will hold: the request's when it sent one (empty clears), else the view's own.
    private static string? Filters(UpdateDataViewRequest request, IDataViewDataSetVariationConfiguration view)
        => request.Filters is null ? view.Filters : (string.IsNullOrWhiteSpace(request.Filters) ? null : request.Filters);

    // A blank string clears the value to null, as it does for the filter; null leaves it alone.
    private static string? Cleared(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    // A null field is left alone: that is what PATCH means.
    private static void Apply(UpdateDataViewRequest request, IDataViewDataSetVariationConfiguration view)
    {
        if (request.DisplayName is { } displayName) view.DisplayName = Cleared(displayName);
        if (request.Description is { } description) view.Description = Cleared(description);
        if (request.Encoding is { } encoding) view.Encoding = encoding;
        if (request.ChartType is { } chartType) view.ChartType = chartType;
        view.Filters = Filters(request, view);
    }
}
