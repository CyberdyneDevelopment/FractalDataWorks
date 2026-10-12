using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Creates a data set a workspace owns as a variation of a main data set: <c>POST /workspaces/{WorkspaceName}/data-sets</c>.
/// </summary>
/// <remarks>
/// <para>
/// A variation is its own record (<see cref="DataSetVariationDomainConfiguration"/>), not a data set: it names the
/// workspace that owns it and the main data set it varies, and it holds no fields of its own. The data route runs the
/// main data set when it is asked for the variation, and no data set list or picker shows it (FDW-1022).
/// </para>
/// <para>
/// The caller needs what attaching the main data set needs (<see cref="IWorkspaceAccessPolicy.MayAttachDataSet"/>),
/// because the variation is that data set's content under the workspace's access.
/// </para>
/// <para>
/// The ownership is the record's <c>OwnerWorkspaceId</c>, so there is nothing to attach and nothing to undo: one
/// write. A variation's name is unique within its workspace, so two workspaces can each own a "Students". The
/// provider identifies a member by <c>Name</c> alone, so <c>Name</c> is the workspace's name and the variation's name
/// joined by <c>/</c>, which is why a variation's own name cannot contain one.
/// </para>
/// </remarks>
public abstract class CreateDataSetVariationEndpointBase
    : CrudCreateEndpointBase<CreateDataSetVariationRequest, DataSetVariationResponse>
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <summary>Initializes a new instance of the <see cref="CreateDataSetVariationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaces">Resolves the workspace that will own the variation.</param>
    /// <param name="access">Decides whether the caller may write the workspace and attach the data set.</param>
    /// <param name="dataSets">Resolves the main data set.</param>
    /// <param name="variations">Finds and saves the variation.</param>
    protected CreateDataSetVariationEndpointBase(
        ILogger<CreateDataSetVariationEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
        _dataSets = dataSets;
        _variations = variations;
    }

    /// <inheritdoc />
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>The access policy decides, as it does for attaching a resource, so the platform permission is not the gate (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{WorkspaceName}/data-sets";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a data set in a workspace as a variation of a main data set";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Creates a data set the workspace owns as a variation of a main data set. The name must be unused by the " +
        "workspace's variations and by every data set. Needs what attaching the main data set needs.";

    /// <inheritdoc />
    protected override string GetResourceName(CreateDataSetVariationRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>
    /// The caller's right to write the workspace is asked before a conflict is reported: a 409 naming a variation the
    /// caller may not touch would confirm it exists. A refused caller goes on to Create, which refuses them with the
    /// policy's own answer. A variation never shares a name with a data set, so the data route's answer for a name is
    /// never ambiguous.
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExists(CreateDataSetVariationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.WorkspaceName))
            return GenericResult<bool>.Success(false);

        var workspace = await _workspaces.Get(request.WorkspaceName, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is null) return GenericResult<bool>.Success(false);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted)) return permitted.ToNewResult<bool>();
        if (permitted.IsFailure) return GenericResult<bool>.Success(false);

        var variation = await _variations.Get($"{workspace.Value.Name}/{request.Name}", ct).ConfigureAwait(false);
        if (variation.IsFailure) return variation.ToNewResult<bool>();
        if (variation.Value is not null) return GenericResult<bool>.Success(true);

        var dataSet = await _dataSets.Get(request.Name, ct).ConfigureAwait(false);
        return dataSet.IsFailure
            ? dataSet.ToNewResult<bool>()
            : GenericResult<bool>.Success(dataSet.Value is not null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> ValidateCreate(CreateDataSetVariationRequest request, CancellationToken ct)
    {
        var invalid = string.IsNullOrWhiteSpace(request.Name) || request.Name.Contains('/', StringComparison.Ordinal) ? "name"
            : string.IsNullOrWhiteSpace(request.BaseDataSetName) && string.IsNullOrWhiteSpace(request.ParentName) ? "baseDataSetName"
            : null;
        if (invalid is not null)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), Logger,
                ResultDetails.Create("name", request.WorkspaceName, "field", invalid, "value", request.Name)));
        }

        return Task.FromResult(DataClassificationValidator.Validate(request.ClassificationId, Logger));
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataSetVariationResponse>> Create(
        CreateDataSetVariationRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.WorkspaceName, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<DataSetVariationResponse>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<DataSetVariationResponse>(request.WorkspaceName, Logger);

        // What the view is a view of: a data set by name, or a view of this workspace by name, whose root data set it shares.
        var based = await DataSetVariationParent.Resolve(_dataSets, _variations, workspace.Value, request, Logger, ct).ConfigureAwait(false);
        if (based.IsFailure) return based.ToNewResult<DataSetVariationResponse>();

        var main = based.Value!.Main;
        var filter = DataViewFilter.Translate(
            request.Filters,
            main.Fields.Where(f => f.IsCurrent && !f.IsDeleted).ToList(),
            new DataViewRefusals(request.Name, Logger, "DataSetVariationInvalid"));
        if (filter.IsFailure) return filter.ToNewResult<DataSetVariationResponse>();

        var permitted = await _access.MayAttachDataSet(workspace.Value, main.Id, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<DataSetVariationResponse>();

        DataSetVariationLog.CreatingVariation(Logger, request.Name, workspace.Value.Name, main.Name);
        var variation = new ScopedDataSetVariationConfiguration
        {
            Name = $"{workspace.Value.Name}/{request.Name}",
            VariationName = request.Name,
            DisplayName = request.DisplayName,
            ClassificationId = request.ClassificationId,
            OwnerWorkspaceId = workspace.Value.Id,
            // The root data set whatever the parent is, so every view of a data set lists with it and a view of a view runs it.
            BaseDataSetId = main.Id,
            ParentVariationId = based.Value.Parent?.Id,
            Filters = request.Filters,
            // Stamped here for the reason the other creates stamp it: the insert writes every mapped column, so an
            // unset date would overwrite the column default (FDW-793).
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var saved = await _variations.Save(variation, "DataSetVariation", ScopedDataSetVariationConfiguration.ImplementationName, variation.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<DataSetVariationResponse>();

        DataSetVariationLog.CreatedVariation(Logger, request.Name, workspace.Value.Name, main.Name);
        return GenericResult<DataSetVariationResponse>.Success(new DataSetVariationResponse
        {
            Id = variation.Id,
            Name = variation.VariationName,
            DisplayName = variation.DisplayName,
            WorkspaceName = workspace.Value.Name,
            BaseDataSetName = main.Name,
            ParentName = based.Value.Parent?.VariationName,
            Filters = variation.Filters,
            ClassificationId = variation.ClassificationId,
        });
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(DataSetVariationResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
