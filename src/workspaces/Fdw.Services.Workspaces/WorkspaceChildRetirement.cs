using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Retires one member, relationship or resource row of a workspace by its Id through the gateway (FDW-7024, FDW-7093).
/// </summary>
public sealed class WorkspaceChildRetirement : IWorkspaceChildRetirement
{
    private const string MemberContainer = "WorkspaceMember";
    private const string RelationshipContainer = "WorkspaceRelationship";
    private const string ResourceContainer = "WorkspaceResource";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly ILogger<WorkspaceChildRetirement> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceChildRetirement"/> class.</summary>
    /// <param name="gateways">Supplies the configuration gateway the workspace rows live on.</param>
    /// <param name="dataStoreName">The configuration connection the workspace rows live on.</param>
    /// <param name="pathName">The schema the workspace rows live in.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceChildRetirement(IConfigurationGatewayProvider gateways, string dataStoreName, string pathName, ILogger<WorkspaceChildRetirement>? logger = null)
    {
        _gateways = gateways;
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _logger = logger ?? NullLogger<WorkspaceChildRetirement>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> RemoveMember(
        IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member, CancellationToken cancellationToken = default)
    {
        var retired = await Retire(workspace.Name, MemberContainer, member.Id, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure) return retired;

        workspace.Members.Remove(member);
        return retired;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DetachRelationship(
        IWorkspaceImplementationConfiguration workspace, WorkspaceRelationshipConfiguration relationship, CancellationToken cancellationToken = default)
    {
        var retired = await Retire(workspace.Name, RelationshipContainer, relationship.Id, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure) return retired;

        workspace.Relationships.Remove(relationship);
        return retired;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DetachResource(
        IWorkspaceImplementationConfiguration workspace, WorkspaceResourceConfiguration resource, CancellationToken cancellationToken = default)
    {
        var retired = await Retire(workspace.Name, ResourceContainer, resource.Id, cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure) return retired;

        workspace.Resources.Remove(resource);
        return retired;
    }

    // One row retired by its Logical key (its Id) on its own container, marked deleted.
    private async Task<IGenericResult> Retire(string workspaceName, string container, Guid childId, CancellationToken cancellationToken)
    {
        if (_pathName.Length == 0)
            return GenericResult.Failure(WorkspaceChildRetirementLog.PathNotDeclared(_logger, _dataStoreName, container));

        var childText = childId.ToString("D", CultureInfo.InvariantCulture);
        var gateway = _gateways.Get(_dataStoreName);
        if (gateway.IsFailure)
            return GenericResult.Failure(WorkspaceChildRetirementLog.RetireFailed(_logger, workspaceName, container, childText, gateway.FailureReason().Message));

        var retired = await gateway.Value!.Execute<ConfigurationWriteResult>(
            new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [childId], markDeleted: true),
            new DataStoreTarget(_dataStoreName, _pathName, container), cancellationToken).ConfigureAwait(false);
        if (retired.IsFailure)
            return GenericResult.Failure(WorkspaceChildRetirementLog.RetireFailed(_logger, workspaceName, container, childText, retired.FailureReason().Message));

        WorkspaceChildRetirementLog.Retired(_logger, workspaceName, container, childText);
        return GenericResult.Success();
    }
}
