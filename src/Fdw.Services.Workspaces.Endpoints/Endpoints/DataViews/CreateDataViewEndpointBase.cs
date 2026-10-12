using System;
using System.Linq;
using System.Text.RegularExpressions;
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

/// <summary>Creates a data view.</summary>
/// <remarks>
/// A data view is a data set variation a user owns (FDW-1032): a view of a data set, or of another view, with its own
/// filter and a chart to draw it with. It is not a workspace's child and has no workspace owner; membership in a workspace
/// is by <c>WorkspaceResource</c>, as for a data set (DataView is a registered WorkspaceResourceKinds option), so
/// attaching a view once created is a separate call to the existing POST /workspaces/{Name}/resources.
/// <para>
/// A view whose chart cannot be drawn is refused when it is created, not when someone opens it (FDW-1023): the chart type
/// must be one of <see cref="ChartTypes"/>, the bindings must fit one way of binding it, every field they name must be a
/// current field of the root data set of an axis its slot accepts (or take the aggregate the binding names), and the
/// stored filter must resolve against that data set the way the data route resolves it.
/// </para>
/// </remarks>
public abstract class CreateDataViewEndpointBase : CrudCreateEndpointBase<CreateDataViewRequest, DataViewResponse>
{
    private const string NamePattern = "^[a-z0-9][a-z0-9-]{1,62}$";

    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected CreateDataViewEndpointBase(
        ILogger<CreateDataViewEndpointBase> logger,
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
    protected override string GetResourceName(CreateDataViewRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateDataViewRequest request, CancellationToken ct)
    {
        var result = await _variations.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The name is letters, digits and dashes, which keeps it from ever meeting the <c>workspace/name</c> form a variation a
    /// workspace owns is keyed by.
    /// </remarks>
    protected override Task<IGenericResult> ValidateCreate(CreateDataViewRequest request, CancellationToken ct)
    {
        var matches = Regex.IsMatch(request.Name, NamePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return Task.FromResult<IGenericResult>(matches
            ? GenericResult.Success()
            : new DataViewRefusals(request.Name, Logger, "DataViewSpecInvalid")
                .Invalid<bool>("the name is 2 to 63 letters, digits and dashes, starting with a letter or digit"));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataViewResponse>> Create(
        CreateDataViewRequest request, CancellationToken ct)
    {
        if (_authContext.Current is not { } caller)
        {
            return GenericResult<DataViewResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "no authentication context"));
        }

        if (!Guid.TryParse(caller.UserId, out var ownerUserId))
        {
            return GenericResult<DataViewResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "UserId is not a Guid"));
        }

        var refusals = new DataViewRefusals(request.Name, Logger, "DataViewSpecInvalid");
        var based = await Based(request, refusals, ct).ConfigureAwait(false);
        if (based.IsFailure) return based.ToNewResult<DataViewResponse>();

        var refused = Refusal(request, based.Value.Main, refusals);
        if (refused is not null) return refused;

        var view = new DataViewDataSetVariationConfiguration
        {
            // The implementation the domain provider is registered under; a new record names it or the save refuses it.
            Implementation = DataViewDataSetVariationConfiguration.ImplementationName,
            Name = request.Name,
            VariationName = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            OwnerUserId = ownerUserId,
            // The ROOT data set whatever the parent is, so every view of a data set lists with it and a view of a view runs it.
            BaseDataSetId = based.Value.Main.Id,
            ParentVariationId = based.Value.ParentId,
            Encoding = request.Encoding,
            Filters = request.Filters,
            ChartType = request.ChartType,
            // Why stamped here rather than left to the column DEFAULT: the insert writes every mapped column,
            // including ones nobody set, so the unset CLR default (0001-01-01) overwrites DEFAULT
            // (sysdatetimeoffset()) rather than deferring to it. (FDW-793)
            CreatedAt = DateTimeOffset.UtcNow,
            CreateDate = DateTimeOffset.UtcNow,
        };

        var saved = await _variations.Save(
            view, "DataSetVariation", DataViewDataSetVariationConfiguration.ImplementationName, view.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<DataViewResponse>();

        var parentName = await DataViewStore.ParentName(_variations, view, Logger, ct).ConfigureAwait(false);
        return parentName.IsFailure
            ? parentName.ToNewResult<DataViewResponse>()
            : GenericResult<DataViewResponse>.Success(DataViewResponseMapper.ToResponse(view, parentName.Value));
    }

    /// <summary>What the new view is a view of: the root data set, and the parent view when it has one.</summary>
    /// <remarks>
    /// A data set by id, or a view by name whose root data set it then shares. A request that names both must name the same
    /// data set. The chain may not pass <see cref="DataSetVariationChain.MaxDepth"/> views, the new one included.
    /// </remarks>
    private async Task<IGenericResult<(IDataSetImplementationConfiguration Main, Guid? ParentId)>> Based(
        CreateDataViewRequest request, DataViewRefusals refusals, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ParentName))
        {
            var named = await _dataSets.Get(request.SubjectDataSetId, ct).ConfigureAwait(false);
            if (named.IsFailure) return named.ToNewResult<(IDataSetImplementationConfiguration, Guid?)>();
            return named.Value is { } dataSet
                ? GenericResult<(IDataSetImplementationConfiguration, Guid?)>.Success((dataSet, null))
                : refusals.Invalid<(IDataSetImplementationConfiguration, Guid?)>($"the subject data set {request.SubjectDataSetId} does not exist");
        }

        var parent = await _variations.Get(request.ParentName, ct).ConfigureAwait(false);
        if (parent.IsFailure) return parent.ToNewResult<(IDataSetImplementationConfiguration, Guid?)>();
        if (parent.Value is not { } view)
            return refusals.Invalid<(IDataSetImplementationConfiguration, Guid?)>($"'{request.ParentName}' is not a view this caller can read");

        var ancestors = await DataSetVariationChain.Ancestors(_variations, view.ParentVariationId, request.ParentName, Logger, ct).ConfigureAwait(false);
        if (ancestors.IsFailure) return ancestors.ToNewResult<(IDataSetImplementationConfiguration, Guid?)>();
        if (ancestors.Value!.Count + 2 > DataSetVariationChain.MaxDepth)
            return refusals.Invalid<(IDataSetImplementationConfiguration, Guid?)>($"a chain of views holds at most {DataSetVariationChain.MaxDepth}, and this one would hold more");
        if (request.SubjectDataSetId != Guid.Empty && request.SubjectDataSetId != view.BaseDataSetId)
            return refusals.Invalid<(IDataSetImplementationConfiguration, Guid?)>($"subjectDataSetId is not the data set of its parent view '{request.ParentName}'");

        var main = await _dataSets.Get(view.BaseDataSetId, ct).ConfigureAwait(false);
        if (main.IsFailure) return main.ToNewResult<(IDataSetImplementationConfiguration, Guid?)>();
        return main.Value is { } root
            ? GenericResult<(IDataSetImplementationConfiguration, Guid?)>.Success((root, view.Id))
            : refusals.Invalid<(IDataSetImplementationConfiguration, Guid?)>($"the data set of its parent view '{request.ParentName}' no longer exists");
    }

    /// <summary>The refusal a view whose chart cannot be drawn is answered with, or null when it can.</summary>
    /// <remarks>
    /// Checked against the root data set as it is now: a field the data set later loses leaves the view saved and the chart
    /// refused when it is queried, by the data route, which names the field.
    /// </remarks>
    private static IGenericResult<DataViewResponse>? Refusal(
        CreateDataViewRequest request, IDataSetImplementationConfiguration main, DataViewRefusals refusals)
    {
        var fields = main.Fields.Where(f => f.IsCurrent && !f.IsDeleted).ToList();
        var reason = ChartSpecificationCheck.Reason(request.ChartType, request.Encoding, fields);
        if (reason is not null) return refusals.Invalid<DataViewResponse>(reason);

        var filter = DataViewFilter.Translate(request.Filters, fields, refusals);
        return filter.IsFailure ? filter.ToNewResult<DataViewResponse>() : null;
    }
}
