using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Lists the workspaces a data set is attached to, or that own a variation of it: <c>GET /datasets/{DataSetName}/workspaces</c>.
/// </summary>
/// <remarks>
/// <para>
/// The reverse of a workspace's resources, for a screen that starts from a data set and needs to know
/// which workspace to query it through (FDW-937). It starts from the data set, so it is gated like the
/// data set: <c>datasets:read</c>, and the set resolved as the caller so row-level security applies. A
/// workspace never makes a data set visible outside itself; a set the caller cannot see answers 404
/// exactly like one that does not exist.
/// </para>
/// <para>
/// Each row is a workspace holding the data set that the caller may view
/// (<see cref="IWorkspaceAccessPolicy.MayView"/>), flagged with whether the data route would serve
/// them rows through it (<see cref="IWorkspaceAccessPolicy.MayRead"/>). A workspace the caller may not
/// view is left out, not shown as refused.
/// </para>
/// <para>
/// A workspace is listed once for each way it holds the data set: attached as itself, or owning a variation of it
/// (FDW-1022). Each row names the data set to ask that workspace's data route for, which is the variation's own
/// name for the second, and says which it is. The variations come from one query on their domain table, by the
/// data set they vary.
/// </para>
/// </remarks>
public abstract class ListDataSetWorkspacesEndpointBase
    : Endpoint<DataSetWorkspacesRequest, DataSetWorkspacesResponse>
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;

    // Why: the most a page may ask for. A data set has few variations, and the list needs all of them.
    private const int AllVariations = int.MaxValue - 1;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetWorkspacesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaces">Supplies every workspace with its members and resources.</param>
    /// <param name="access">Decides what the caller may do with each workspace.</param>
    /// <param name="dataSets">Resolves the data set by name, as the caller.</param>
    /// <param name="variations">Finds the variations of the data set.</param>
    protected ListDataSetWorkspacesEndpointBase(
        ILogger<ListDataSetWorkspacesEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations)
    {
        Logger = logger ?? NullLogger<ListDataSetWorkspacesEndpointBase>.Instance;
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _variations = variations;
    }

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/datasets/{DataSetName}/workspaces");
        Policies("datasets:read");
        Tags("Workspaces");
        Summary(s =>
        {
            s.Summary = "List the workspaces a data set is attached to";
            s.Description =
                "The workspaces holding the data set that the caller may view, each with canQuery " +
                "(whether POST /{workspace}/{dataSet} would serve rows). A workspace that owns a variation of the " +
                "data set is listed with the variation's name in dataSetName and isVariation true. Needs " +
                "datasets:read; a data set the caller cannot see answers 404.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetWorkspacesRequest req, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        EndpointLogger.ExecutingEndpoint(Logger, GetType().Name, nameof(DataSetWorkspacesRequest));

        var response = await List(req.DataSetName, ct).ConfigureAwait(false);
        if (response.IsFailure)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(response, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response.Value!, ct).ConfigureAwait(false);
        EndpointLogger.EndpointExecuted(Logger, GetType().Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private async Task<IGenericResult<DataSetWorkspacesResponse>> List(string dataSetName, CancellationToken ct)
    {
        var dataSetId = await VisibleDataSetId(dataSetName, ct).ConfigureAwait(false);
        if (dataSetId.IsFailure) return dataSetId.ToNewResult<DataSetWorkspacesResponse>();

        var all = await _workspaces.Get(ct).ConfigureAwait(false);
        if (all.IsFailure) return all.ToNewResult<DataSetWorkspacesResponse>();
        if (all.Value is not { } candidates)
        {
            return GenericResult<DataSetWorkspacesResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", "*"));
        }

        var varied = await VariationsOf(dataSetId.Value, ct).ConfigureAwait(false);
        if (varied.IsFailure) return varied.ToNewResult<DataSetWorkspacesResponse>();

        var rows = new List<DataSetWorkspaceDto>();
        foreach (var (workspace, heldName, isVariation) in Held(candidates, dataSetId.Value, dataSetName, varied.Value!))
        {
            var row = await Row(workspace, heldName, isVariation, ct).ConfigureAwait(false);
            if (row.IsFailure) return row.ToNewResult<DataSetWorkspacesResponse>();
            if (row.Value is { } found) rows.Add(found);
        }

        return GenericResult<DataSetWorkspacesResponse>.Success(new DataSetWorkspacesResponse
        {
            DataSetName = dataSetName,
            Workspaces =
            [
                .. rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.DataSetName, StringComparer.OrdinalIgnoreCase),
            ],
        });
    }

    /// <summary>The variations of the data set, read as domain rows in one query.</summary>
    private async Task<IGenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>> VariationsOf(
        Guid dataSetId, CancellationToken ct)
    {
        var page = await _variations.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(DataSetVariationDomainConfiguration.BaseDataSetId),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = dataSetId,
                    },
                },
                null, 0, AllVariations, false),
            ct).ConfigureAwait(false);
        if (page.IsFailure) return page.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>();

        var rows = new List<DataSetVariationDomainConfiguration>();
        await foreach (var row in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
            rows.Add(row);

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<IReadOnlyList<DataSetVariationDomainConfiguration>>()
            : GenericResult<IReadOnlyList<DataSetVariationDomainConfiguration>>.Success(rows);
    }

    /// <summary>Each way a workspace holds the data set: attached as itself, or owning a variation of it.</summary>
    /// <remarks>A data view is a variation of the data set too, but a user owns it, so no workspace holds it and it is skipped here.</remarks>
    private static IEnumerable<(IWorkspaceImplementationConfiguration Workspace, string DataSetName, bool IsVariation)> Held(
        IReadOnlyList<IWorkspaceImplementationConfiguration> candidates,
        Guid dataSetId,
        string dataSetName,
        IReadOnlyList<DataSetVariationDomainConfiguration> variations)
    {
        foreach (var workspace in candidates.Where(d => Holds(d, dataSetId)))
            yield return (workspace, dataSetName, false);

        foreach (var variation in variations)
        {
            if (candidates.FirstOrDefault(d => d.Id == variation.OwnerWorkspaceId) is { } owner)
                yield return (owner, variation.VariationName, true);
        }
    }

    /// <summary>Resolves the data set's id as the caller, answering 404 for one that is absent or hidden by row-level security.</summary>
    private async Task<IGenericResult<Guid>> VisibleDataSetId(string dataSetName, CancellationToken ct)
    {
        var dataSet = await _dataSets.Get(dataSetName, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<Guid>();

        return dataSet.Value is { } found
            ? GenericResult<Guid>.Success(found.Id)
            : GenericResult<Guid>.Failure(
                WorkspacesResultCodes.ByName("DataSetNotVisible"), Logger,
                ResultDetails.Create("dataSet", dataSetName));
    }

    /// <summary>The caller's row for one workspace; a null value when they may not view it.</summary>
    /// <remarks>
    /// Refusals are answers here, not errors: the policy has logged each one. An access decision that
    /// could not be made is an error, and fails the whole lookup rather than dropping a row (FDW-937).
    /// </remarks>
    private async Task<IGenericResult<DataSetWorkspaceDto?>> Row(
        IWorkspaceImplementationConfiguration workspace, string dataSetName, bool isVariation, CancellationToken ct)
    {
        var viewable = await _access.MayView(workspace, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(viewable)) return viewable.ToNewResult<DataSetWorkspaceDto?>();
        if (viewable.IsFailure) return GenericResult<DataSetWorkspaceDto?>.Success(null);

        var queryable = await _access.MayRead(workspace, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(queryable)) return queryable.ToNewResult<DataSetWorkspaceDto?>();

        return GenericResult<DataSetWorkspaceDto?>.Success(new DataSetWorkspaceDto
        {
            Name = workspace.Name,
            DisplayName = workspace.DisplayName,
            DataSetName = dataSetName,
            IsVariation = isVariation,
            CanQuery = queryable.IsSuccess,
        });
    }

    private static bool Holds(IWorkspaceImplementationConfiguration workspace, Guid dataSetId) =>
        workspace.Resources.Any(r =>
            r.IsCurrent && !r.IsDeleted
            && r.ResourceId == dataSetId
            && string.Equals(r.ResourceType, "DataSet", StringComparison.Ordinal));
}
