using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Default <see cref="IWorkspaceReadService"/>: the workspace provider, its access policy and the data
/// set provider, asked in the order that loads nothing outside the caller's grant.
/// </summary>
/// <remarks>
/// Scoped, because the policy reads the calling user off the ambient authentication context.
/// </remarks>
public sealed class WorkspaceReadService : IWorkspaceReadService
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadService"/> class.</summary>
    /// <param name="workspaces">Resolves workspaces with their members and resources.</param>
    /// <param name="access">Decides whether the caller may read a workspace.</param>
    /// <param name="dataSets">Resolves the attached data sets and their fields.</param>
    /// <param name="variations">Finds the variations a workspace owns, and the data set each varies.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceReadService(
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets,
        IDataSetVariationConfigurationProvider variations,
        ILogger<WorkspaceReadService>? logger = null)
    {
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dataSets = dataSets ?? throw new ArgumentNullException(nameof(dataSets));
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
        _logger = logger ?? NullLogger<WorkspaceReadService>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>> Readable(
        CancellationToken cancellationToken = default)
        => ReadableWorkspaces.Get(_workspaces, _access, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<IWorkspaceImplementationConfiguration>> Readable(
        string workspaceName, CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaces.Get(workspaceName, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace;
        if (workspace.Value is null) return NotReadable(workspaceName);

        var permitted = await _access.MayRead(workspace.Value, cancellationToken).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted))
            return permitted.ToNewResult<IWorkspaceImplementationConfiguration>();

        // The policy has already logged the refusal and its reason. The caller is told only what an
        // absent workspace would tell them.
        return permitted.IsFailure
            ? NotReadable(workspaceName)
            : GenericResult<IWorkspaceImplementationConfiguration>.Success(workspace.Value);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<ReadableDataSet>>> DataSets(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        WorkspaceReadLog.ListingDataSets(_logger, workspace.Name);

        var readable = new List<ReadableDataSet>();
        foreach (var attachment in workspace.Resources.Where(IsCurrentDataSet))
        {
            var dataSet = await _dataSets.Get(attachment.ResourceId, cancellationToken).ConfigureAwait(false);
            if (dataSet.IsFailure || dataSet.Value is null)
            {
                WorkspaceReadLog.AttachmentLeftOut(_logger, workspace.Name, attachment.Name);
                continue;
            }

            readable.Add(new ReadableDataSet(dataSet.Value.Name, dataSet.Value, []));
        }

        // The variations the workspace owns are read as data sets too, under the names the workspace knows them by.
        var owned = await DataSetVariationQueries.OwnedBy(_variations, workspace.Id, cancellationToken).ConfigureAwait(false);
        if (owned.IsFailure) return owned.ToNewResult<IReadOnlyList<ReadableDataSet>>();

        foreach (var variation in owned.Value!)
        {
            var varied = await VariedDataSet(variation, cancellationToken).ConfigureAwait(false);
            if (varied.IsFailure)
            {
                WorkspaceReadLog.AttachmentLeftOut(_logger, workspace.Name, variation.VariationName);
                continue;
            }

            readable.Add(varied.Value!);
        }

        return GenericResult<IReadOnlyList<ReadableDataSet>>.Success(readable);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ReadableDataSet>> DataSet(
        string workspaceName, string dataSetName, CancellationToken cancellationToken = default)
    {
        WorkspaceReadLog.ResolvingDataSet(_logger, workspaceName, dataSetName);

        var workspace = await Readable(workspaceName, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<ReadableDataSet>();

        var variation = await DataSetVariationQueries.Named(_variations, workspace.Value!.Id, dataSetName, cancellationToken).ConfigureAwait(false);
        if (variation.IsFailure) return variation.ToNewResult<ReadableDataSet>();

        var found = variation.Value is { } varied
            ? await VariedDataSet(varied, cancellationToken).ConfigureAwait(false)
            : await FindAttachedDataSet(workspace.Value!, dataSetName, cancellationToken).ConfigureAwait(false);
        if (found.IsSuccess)
            WorkspaceReadLog.DataSetResolved(_logger, workspaceName, dataSetName, found.Value!.ExecutionTarget.Name);

        return found;
    }

    /// <summary>Walks the workspace's DataSet attachments and returns the one whose data set carries the requested name.</summary>
    /// <remarks>
    /// Resolved from the attachments rather than by name first: a lookup by name would load, and on a
    /// broken configuration report an error about, a data set outside the grant. Attachments are few
    /// and the provider caches each read, so walking them costs little.
    /// <para>
    /// An attachment that fails to load does not stop the walk — one broken data set must not take
    /// every other data set in the workspace down with it. Its failure is what the caller gets only
    /// when nothing else matched, since the one that failed may have been the one asked for.
    /// </para>
    /// </remarks>
    private async Task<IGenericResult<ReadableDataSet>> FindAttachedDataSet(
        IWorkspaceImplementationConfiguration workspace, string dataSetName, CancellationToken cancellationToken)
    {
        IGenericResult<IDataSetImplementationConfiguration>? firstFailure = null;
        foreach (var attachment in workspace.Resources.Where(IsCurrentDataSet))
        {
            var dataSet = await _dataSets.Get(attachment.ResourceId, cancellationToken).ConfigureAwait(false);
            if (dataSet.IsFailure)
            {
                if (firstFailure is null) firstFailure = dataSet;
                continue;
            }

            if (dataSet.Value is not { } found || !string.Equals(found.Name, dataSetName, StringComparison.OrdinalIgnoreCase))
                continue;

            return GenericResult<ReadableDataSet>.Success(new ReadableDataSet(found.Name, found, []));
        }

        if (firstFailure is not null) return firstFailure.ToNewResult<ReadableDataSet>();

        return GenericResult<ReadableDataSet>.Failure(
            WorkspacesResultCodes.ByName("WorkspaceDataSetNotAttached"), _logger,
            ResultDetails.Create("name", workspace.Name, "dataSet", dataSetName));
    }

    /// <summary>The data set a variation runs: the main data set it varies, read as the caller, under the variation's own name.</summary>
    /// <remarks>
    /// A variation holds no fields or sources of its own, so the query runs the main data set it varies, under
    /// the access the workspace gave, narrowed by the stored filter of the view and of every view above it. The main data set is read as the caller: one row-level security hides
    /// answers like an absent data set, so a variation never confirms what the caller cannot see.
    /// </remarks>
    private async Task<IGenericResult<ReadableDataSet>> VariedDataSet(
        DataSetVariationDomainConfiguration variation, CancellationToken cancellationToken)
    {
        var main = await _dataSets.Get(variation.BaseDataSetId, cancellationToken).ConfigureAwait(false);
        if (main.IsFailure) return main.ToNewResult<ReadableDataSet>();

        if (main.Value is not { } found)
            return GenericResult<ReadableDataSet>.Failure(
                WorkspacesResultCodes.ByName("DataSetNotVisible"), _logger,
                ResultDetails.Create("dataSet", variation.VariationName));

        var filters = await DataSetVariationChain.StoredFilters(_variations, variation, _logger, cancellationToken).ConfigureAwait(false);
        return filters.IsFailure
            ? filters.ToNewResult<ReadableDataSet>()
            : GenericResult<ReadableDataSet>.Success(new ReadableDataSet(variation.VariationName, found, filters.Value!));
    }

    private static bool IsCurrentDataSet(WorkspaceResourceConfiguration resource) =>
        resource.IsCurrent && !resource.IsDeleted
        && string.Equals(resource.ResourceType, "DataSet", StringComparison.Ordinal);

    private IGenericResult<IWorkspaceImplementationConfiguration> NotReadable(string workspaceName)
        => GenericResult<IWorkspaceImplementationConfiguration>.Failure(
            WorkspacesResultCodes.ByName("WorkspaceNotReadable"), _logger,
            ResultDetails.Create("name", workspaceName));
}
